using System;
using System.Threading.Tasks;
using NanoMessageBus.Logging;

namespace NanoMessageBus.Channels
{
	public class PooledDispatchChannel : IMessagingChannel
	{
		public virtual bool Active => _channel.Active;

	    public virtual ChannelMessage CurrentMessage => null;

	    public virtual IDependencyResolver CurrentResolver => _channel.CurrentResolver;

	    public virtual IChannelTransaction CurrentTransaction => _channel.CurrentTransaction;

	    public virtual IChannelGroupConfiguration CurrentConfiguration => _channel.CurrentConfiguration;

	    public virtual IDispatchContext PrepareDispatch(object message = null, IMessagingChannel actual = null)
		{
			Log.Debug("Preparing a dispatch");
			return _channel.PrepareDispatch(message, actual ?? this);
		}

		public virtual Task SendAsync(ChannelEnvelope envelope)
		{
			if (envelope == null)
			{
			    throw new ArgumentNullException(nameof(envelope));
			}

		    ThrowWhenDisposed();
			return TrySend(envelope);
		}

		protected virtual async Task TrySend(ChannelEnvelope envelope)
		{
			try
			{
				Log.Verbose("Sending envelope '{0}' through the underlying channel.", envelope.MessageId());
				await _channel.SendAsync(envelope).ConfigureAwait(false);
			}
			catch (ChannelConnectionException)
			{
				Log.Info("Channel is unavailable, tearing down the channel.");
				_connector.Teardown(_channel, _token);
				throw;
			}
		}

		public virtual Task ShutdownAsync()
		{
			Log.Error("This channel does not support asynchronous operations.");
			throw new NotSupportedException();
		}

		public virtual Task ReceiveAsync(Func<IDeliveryContext, Task> callback)
		{
			Log.Error("This channel does not support asynchronous operations.");
			throw new NotSupportedException();
		}

		protected virtual void ThrowWhenDisposed()
		{
			if (!_disposed)
			{
			    return;
			}

		    Log.Warn("The channel has been disposed.");
			throw new ObjectDisposedException(typeof(PooledDispatchChannel).Name);
		}

		public PooledDispatchChannel(PooledDispatchConnector connector, IMessagingChannel channel, int token)
		{
			if (connector == null)
			{
			    throw new ArgumentNullException(nameof(connector));
			}

		    if (channel == null)
		    {
		        throw new ArgumentNullException(nameof(channel));
		    }

		    if (token < 0)
		    {
		        throw new ArgumentException("The token greater than or equal to zero.", nameof(token));
		    }

		    _connector = connector;
			_channel = channel;
			_token = token;
		}

		~PooledDispatchChannel()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposing || _disposed)
			{
			    return;
			}

		    Log.Verbose("Releasing the channel back to the pool for later use.");
			_disposed = true;
			_connector.Release(_channel, _token);
		}

		private static readonly ILog Log = LogFactory.Build(typeof(PooledDispatchChannel));
		private readonly PooledDispatchConnector _connector;
		private readonly IMessagingChannel _channel;
		private bool _disposed;
		private readonly int _token;
	}
}