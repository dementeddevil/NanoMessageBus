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
				this.workers.StartSingleWorker(() => this.ReestablishConnection(0));
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

			this.TryDispatch(envelope, completed);
		}
		protected virtual void TryDispatch(ChannelEnvelope envelope, Action<IChannelTransaction> completed)
		{
			this.workers.Add(x =>
			{
				try
				{
					x.Send(envelope);
					completed(x.CurrentTransaction);
				}
				catch (ChannelConnectionException)
				{
					this.workers.Stop();
					this.workers.StartSingleWorker(() => this.ReestablishConnection(0));
				}
			});
		}

		public virtual void BeginReceive(Action<IDeliveryContext> callback)
		{
			if (callback == null)
				throw new ArgumentNullException("callback");

			this.ThrowWhenDisposed();
			this.ThrowWhenUninitialized();
			this.ThrowWhenAlreadyReceiving();
			this.ThrowWhenDispatchOnly();

			lock (this.locker)
				this.receiving = this.TryReceive(callback);
		}
		protected virtual bool TryReceive(Action<IDeliveryContext> callback)
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

				return true;
			}
			catch (ChannelConnectionException)
			{
				return false;
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
			if (this.receiving)
				throw new InvalidOperationException("A callback has already been provided.");
		}

		public DefaultChannelGroup(
			IChannelConnector connector, IChannelGroupConfiguration configuration, IWorkerGroup workers)
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

			this.disposed = true;
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
		private readonly IWorkerGroup workers;
		private bool receiving;
		private bool initialized;
		private bool disposed;
	}
}