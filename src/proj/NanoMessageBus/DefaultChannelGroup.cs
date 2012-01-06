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

				// TODO: throw when disposed

				this.initialized = true;
				this.TryInitialize();
			}
		}
		protected virtual void TryInitialize()
		{
			try
			{
				this.TryConnect();
			}
			catch (ChannelConnectionException)
			{
				// should we even worry about trying to re-establish a connection here?
				// if we let the re-establish occur only on BeginDispatch/Receive ordering
				// is much easier; in the case of a dispatch-only group, we could probably try
				// to re-establish a connection here; for full-duplex we should let the
				// BeginReceive do the re-establish connection loop
				this.workers.RestartWorkers();
			}
		}
		protected virtual void TryConnect()
		{
			using (this.connector.Connect(this.configuration.GroupName)) { }
		}
		protected virtual void ReestablishConnection(int timeoutIndex)
		{
			this.GetTimeout(timeoutIndex).Sleep();

			try
			{
				this.TryConnect();
			}
			catch (ChannelConnectionException)
			{
				this.ReestablishConnection(timeoutIndex + 1);
			}
		}
		protected virtual TimeSpan GetTimeout(int timeoutIndex)
		{
			return timeoutIndex < RetryDelay.Length ? RetryDelay[timeoutIndex] : RetryDelay[RetryDelay.Length - 1];
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

			this.workers.EnqueueWork(x => this.TryChannel(() =>
			{
				x.Send(envelope);
				completed(x.CurrentTransaction);
			}));
		}
		protected virtual void TryChannel(Action callback)
		{
			try
			{
				callback();
			}
			catch (ChannelConnectionException)
			{
				this.workers.RestartWorkers();
			}
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

				// on ChannelConnectionException:
				// 1. stop the workers
				// 2. try to re-establish connection
				// 3. once re-connected,
				// 4. check to see if we're disposed (ensure proper locking)
				// 5. start workers again using callback that started workers listening
				//    (which should contain this set of steps as well?)
				this.TryReceive(callback);
			}
		}
		protected virtual void TryReceive(Action<IDeliveryContext> callback)
		{
			try
			{
				for (var i = 0; i < this.configuration.MinWorkers; i++)
				{
					using (var channel = this.connector.Connect(this.configuration.GroupName))
					{
						channel.Receive(callback);
					}
				}
			}
			catch (ChannelConnectionException)
			{
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
			IChannelConnector connector,
			IChannelGroupConfiguration configuration,
			IWorkerGroup<IMessagingChannel> workers)
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

				this.disposed = true; // TODO: stop workers
			}
		}

		private static readonly TimeSpan[] RetryDelay =
		{
			TimeSpan.FromSeconds(0),
			TimeSpan.FromSeconds(1),
			TimeSpan.FromSeconds(2),
			TimeSpan.FromSeconds(4),
			TimeSpan.FromSeconds(8),
			TimeSpan.FromSeconds(16),
			TimeSpan.FromSeconds(32)
		};
		private readonly object locker = new object();
		private readonly IChannelConnector connector;
		private readonly IChannelGroupConfiguration configuration;
		private readonly IWorkerGroup<IMessagingChannel> workers;
		private Action<IDeliveryContext> receiving;
		private bool initialized;
		private bool disposed;
	}
}