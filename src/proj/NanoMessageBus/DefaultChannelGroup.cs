namespace NanoMessageBus
{
	using System;

	public class DefaultChannelGroup : IChannelGroup
	{
		public virtual bool DispatchOnly
		{
			get { return this.configuration.DispatchOnly; }
		}
		public virtual void Initialize()
		{
			this.initialized = true;
		}
		public virtual void BeginDispatch(ChannelEnvelope envelope, Action<IChannelTransaction> completed)
		{
			if (envelope == null)
				throw new ArgumentNullException("envelope");

			if (completed == null)
				throw new ArgumentNullException("completed");

			this.ThrowWhenDisposed();
			this.ThrowWhenUninitialized();
			this.ThrowWhenFullDuplex(); // TODO: hand it off
		}
		public virtual void BeginReceive(Action<IDeliveryContext> callback)
		{
			if (callback == null)
				throw new ArgumentNullException("callback");

			this.ThrowWhenDisposed();
			this.ThrowWhenUninitialized();
			this.ThrowWhenReceiving();
			this.ThrowWhenDispatchOnly();

			this.receiving = true; // TODO: hand the callback to the channels
		}

		protected virtual void ThrowWhenDisposed()
		{
			if (this.disposed)
				throw new ObjectDisposedException(typeof(DefaultMessagingHost).Name);
		}
		protected virtual void ThrowWhenUninitialized()
		{
			if (!this.initialized)
				throw new InvalidOperationException("The host has not been initialized.");
		}
		protected virtual void ThrowWhenFullDuplex()
		{
			if (!this.configuration.DispatchOnly)
				throw new InvalidOperationException("Dispatch can only be performed using a dispatch-only channel group.");
		}
		protected virtual void ThrowWhenDispatchOnly()
		{
			if (this.configuration.DispatchOnly)
				throw new InvalidOperationException("Dispatch-only channel groups cannot receive messages.");
		}
		protected virtual void ThrowWhenReceiving()
		{
			if (this.receiving)
				throw new InvalidOperationException("A callback has already been provided.");
		}

		public DefaultChannelGroup(IChannelConnector connector, IChannelConfiguration configuration)
		{
			this.connector = connector;
			this.configuration = configuration;
		}
		~DefaultChannelGroup()
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
			if (!disposing)
				return;

			this.disposed = true;
		}

		private readonly IChannelConnector connector;
		private readonly IChannelConfiguration configuration;
		private bool receiving;
		private bool initialized;
		private bool disposed;
	}
}