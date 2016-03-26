namespace NanoMessageBus.Channels
{
	using System;
	using Logging;

	public class PooledDispatchChannel : IMessagingChannel
	{
		public virtual bool Active
		{
			get { return this._channel.Active; }
		}
		public virtual ChannelMessage CurrentMessage
		{
			get { return null; }
		}
		public virtual IDependencyResolver CurrentResolver
		{
			get { return this._channel.CurrentResolver; }
		}
		public virtual IChannelTransaction CurrentTransaction
		{
			get { return this._channel.CurrentTransaction; }
		}
		public virtual IChannelGroupConfiguration CurrentConfiguration
		{
			get { return this._channel.CurrentConfiguration; }
		}

		public virtual IDispatchContext PrepareDispatch(object message = null, IMessagingChannel actual = null)
		{
			Log.Debug("Preparing a dispatch");
			return this._channel.PrepareDispatch(message, actual ?? this);
		}
		public virtual void Send(ChannelEnvelope envelope)
		{
			if (envelope == null)
				throw new ArgumentNullException(nameof(envelope));

			this.ThrowWhenDisposed();
			this.TrySend(envelope);
		}
		protected virtual void TrySend(ChannelEnvelope envelope)
		{
			try
			{
				Log.Verbose("Sending envelope '{0}' through the underlying channel.", envelope.MessageId());
				this._channel.Send(envelope);
			}
			catch (ChannelConnectionException)
			{
				Log.Info("Channel is unavailable, tearing down the channel.");
				this._connector.Teardown(this._channel, this._token);
				throw;
			}
		}

		public virtual void BeginShutdown()
		{
			Log.Error("This channel does not support asynchronous operations.");
			throw new NotSupportedException();
		}
		public virtual void Receive(Action<IDeliveryContext> callback)
		{
			Log.Error("This channel does not support asynchronous operations.");
			throw new NotSupportedException();
		}

		protected virtual void ThrowWhenDisposed()
		{
			if (!this._disposed)
				return;

			Log.Warn("The channel has been disposed.");
			throw new ObjectDisposedException(typeof(PooledDispatchChannel).Name);
		}

		public PooledDispatchChannel(PooledDispatchConnector connector, IMessagingChannel channel, int token)
		{
			if (connector == null)
				throw new ArgumentNullException(nameof(connector));

			if (channel == null)
				throw new ArgumentNullException(nameof(channel));

			if (token < 0)
				throw new ArgumentException("The token greater than or equal to zero.", nameof(token));

			this._connector = connector;
			this._channel = channel;
			this._token = token;
		}
		~PooledDispatchChannel()
		{
			this.Dispose(false);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing || this._disposed)
				return;

			Log.Verbose("Releasing the channel back to the pool for later use.");
			this._disposed = true;
			this._connector.Release(this._channel, this._token);
		}

		private static readonly ILog Log = LogFactory.Build(typeof(PooledDispatchChannel));
		private readonly PooledDispatchConnector _connector;
		private readonly IMessagingChannel _channel;
		private bool _disposed;
		private readonly int _token;
	}
}