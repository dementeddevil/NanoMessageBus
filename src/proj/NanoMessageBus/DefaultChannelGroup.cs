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
			lock (this.sync)
			{
				if (this.initialized)
					return;

				this.initialized = true;
				this.ThrowWhenDisposed();

				this.workers.Initialize(this.Connect, this.TryConnect);

				if (this.TryConnect() && this.DispatchOnly)
					this.workers.StartQueue();
			}
		}
		protected virtual IMessagingChannel Connect()
		{
			// if this ever throws, it's executed within the context of a worker under a TryOperation callback
			return this.connector.Connect(this.configuration.GroupName); // thus causing cancellation and retry
		}
		protected virtual bool TryConnect()
		{
			try
			{
				using (this.Connect())
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

			this.workers.Enqueue(worker => this.TryOperation(() =>
			{
				var channel = worker.State;
				channel.Send(envelope);
				completed(channel.CurrentTransaction);
			}));
		}
		public virtual void BeginReceive(Action<IDeliveryContext> callback)
		{
			if (callback == null)
				throw new ArgumentNullException("callback");

			lock (this.sync)
			{
				this.ThrowWhenDisposed();
				this.ThrowWhenUninitialized();
				this.ThrowWhenAlreadyReceiving();
				this.ThrowWhenDispatchOnly();

				this.receiving = true;
				this.workers.StartActivity(worker => this.TryOperation(() =>
					worker.State.Receive(context => worker.PerformOperation(() => callback(context)))));
			}
		}
		protected virtual void TryOperation(Action callback)
		{
			try
			{
				callback();
			}
			catch (ChannelConnectionException)
			{
				this.TryOperation(this.workers.Restart); // may already be disposed
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
				throw new InvalidOperationException("The channel group has not been initialized.");
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
			if (this.receiving)
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

			lock (this.sync)
			{
				if (this.disposed)
					return;

				this.disposed = true;
				this.workers.Dispose();
			}
		}

		private readonly object sync = new object();
		private readonly IChannelConnector connector;
		private readonly IChannelGroupConfiguration configuration;
		private readonly IWorkerGroup<IMessagingChannel> workers;
		private bool receiving;
		private bool initialized;
		private bool disposed;
	}
}