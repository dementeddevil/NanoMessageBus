namespace NanoMessageBus.Channels
{
	using System;
	using Logging;

	public class PooledDispatchChannel : IMessagingChannel
	{
		public virtual int State
		{
			get { return this.state; }
		}
		public virtual IMessagingChannel Channel
		{
			get { return this.channel; }
		}

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
			this.channel.Send(envelope);
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

		public PooledDispatchChannel(PooledDispatchConnector connector, IMessagingChannel channel, int state)
		{
			if (connector == null)
				throw new ArgumentNullException("connector");

			if (channel == null)
				throw new ArgumentNullException("channel");

			if (state < 0)
				throw new ArgumentException("State greater than or equal to zero.");

			this.connector = connector;
			this.channel = channel;
			this.state = state;
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
			this.connector.Release(this);
		}

		private static readonly ILog Log = LogFactory.Build(typeof(PooledDispatchChannel)); // TODO
		private readonly PooledDispatchConnector connector;
		private readonly IMessagingChannel channel;
		private bool disposed;
		private readonly int state;
	}
}