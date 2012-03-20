namespace NanoMessageBus.Channels
{
	using System;
	using Logging;

	public class PooledDispatchChannel : IMessagingChannel
	{
		public virtual string GroupName
		{
			get { return this.channel.GroupName; }
		}
		public virtual ChannelMessage CurrentMessage
		{
			get { return null; }
		}
		public virtual IDependencyResolver CurrentResolver
		{
			get { return this.channel.CurrentResolver; }
		}
		public virtual IChannelTransaction CurrentTransaction
		{
			get { return this.channel.CurrentTransaction; }
		}
		public virtual IChannelGroupConfiguration CurrentConfiguration
		{
			get { return this.channel.CurrentConfiguration; }
		}

		public virtual IDispatchContext PrepareDispatch(object message = null)
		{
			return this.channel.PrepareDispatch(message);
		}
		public virtual void Send(ChannelEnvelope envelope)
		{
			if (envelope == null)
				throw new ArgumentNullException("envelope");

			this.ThrowWhenDisposed();
			this.TrySend(envelope);
		}
		protected virtual void TrySend(ChannelEnvelope envelope)
		{
			try
			{
				this.channel.Send(envelope);
			}
			catch (ChannelConnectionException)
			{
				this.connector.Teardown(this.channel, this.token);
				throw;
			}
		}

		public virtual void BeginShutdown()
		{
			throw new NotSupportedException();
		}
		public virtual void Receive(Action<IDeliveryContext> callback)
		{
			throw new NotSupportedException();
		}

		protected virtual void ThrowWhenDisposed()
		{
			if (!this.disposed)
				return;

			Log.Warn("The channel has been disposed.");
			throw new ObjectDisposedException(typeof(PooledDispatchChannel).Name);
		}

		public PooledDispatchChannel(PooledDispatchConnector connector, IMessagingChannel channel, int token)
		{
			if (connector == null)
				throw new ArgumentNullException("connector");

			if (channel == null)
				throw new ArgumentNullException("channel");

			if (token < 0)
				throw new ArgumentException("The token greater than or equal to zero.", "token");

			this.connector = connector;
			this.channel = channel;
			this.token = token;
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
			if (!disposing || this.disposed)
				return;

			this.disposed = true;
			this.connector.Release(this.channel, this.token);
		}

		private static readonly ILog Log = LogFactory.Build(typeof(PooledDispatchChannel)); // TODO
		private readonly PooledDispatchConnector connector;
		private readonly IMessagingChannel channel;
		private bool disposed;
		private readonly int token;
	}
}