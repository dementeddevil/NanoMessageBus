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
			lock (this.locker)
			{
				if (this.initialized)
					return;

				this.initialized = true;
				this.ThrowWhenDisposed();

				this.TryInitialize();
			}
		}
		protected virtual void TryInitialize()
		{
			this.workers.Initialize(this.OpenChannel, this.TryChannel);

			try
			{
				using (this.OpenChannel())
					if (this.DispatchOnly)
						this.workers.StartQueue();
			}
			catch (ChannelConnectionException)
			{
			}
		}
		protected virtual IMessagingChannel OpenChannel()
		{
			return this.connector.Connect(this.configuration.GroupName);
		}
		protected virtual bool TryChannel()
		{
			try
			{
				using (this.OpenChannel())
					return true;
			}
			catch (ChannelConnectionException)
			{
				return false;
			}
			catch (ObjectDisposedException)
			{
				return false;
			}
		}

		public virtual void BeginDispatch(ChannelEnvelope envelope, Action<IChannelTransaction> completed)
		{
			if (envelope == null)
				throw new ArgumentNullException("envelope");

			if (completed == null)
				throw new ArgumentNullException("completed");

			this.ThrowWhenDisposed();
			this.ThrowWhenUninitialized();
			this.ThrowWhenFullDuplex();

			this.workers.Enqueue(x => this.TryChannel(() =>
			{
				x.Send(envelope);
				completed(x.CurrentTransaction);
			}));
		}
		public virtual void BeginReceive(Action<IDeliveryContext> callback)
		{
			if (callback == null)
				throw new ArgumentNullException("callback");

			lock (this.locker)
			{
				this.ThrowWhenDisposed();
				this.ThrowWhenUninitialized();
				this.ThrowWhenAlreadyReceiving();
				this.ThrowWhenDispatchOnly();

				this.receiving = callback;

				this.workers.StartActivity(x =>
					this.TryChannel(() => x.Receive(callback)));
			}
		}
		protected virtual void TryChannel(Action callback)
		{
			try
			{
				callback();
			}
			catch (ChannelConnectionException)
			{
				this.TryRestart();
			}
			catch (ObjectDisposedException)
			{
				// no op
			}
		}
		protected virtual void TryRestart()
		{
			try
			{
				this.workers.Restart();
			}
			catch (ObjectDisposedException)
			{
				// no op
			}
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
		protected virtual void ThrowWhenAlreadyReceiving()
		{
			if (this.receiving != null)
				throw new InvalidOperationException("A callback has already been provided.");
		}

		public DefaultChannelGroup(
			IChannelConnector connector, IChannelGroupConfiguration configuration, IWorkerGroup<IMessagingChannel> workers)
		{
			this.connector = connector;
			this.configuration = configuration;
			this.workers = workers;
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

			lock (this.locker)
			{
				if (this.disposed)
					return;

				this.disposed = true;
				this.workers.Dispose();
			}
		}

		private readonly object locker = new object();
		private readonly IChannelConnector connector;
		private readonly IChannelGroupConfiguration configuration;
		private readonly IWorkerGroup<IMessagingChannel> workers;
		private Action<IDeliveryContext> receiving;
		private bool initialized;
		private bool disposed;
	}
}