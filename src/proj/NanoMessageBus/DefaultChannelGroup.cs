namespace NanoMessageBus
{
	using System;
	using Logging;

	public class DefaultChannelGroup : IChannelGroup
	{
		public virtual bool DispatchOnly
		{
			get { return this.configuration.DispatchOnly; }
		}

		public virtual void Initialize()
		{
			Log.Info("Initializing channel group '{0}'.", this.configuration.GroupName);

			lock (this.sync)
			{
				Log.Verbose("Entering critical section (Initialize).");
				if (this.initialized)
				{
					Log.Verbose("Exiting critical section (Initialize)--already initialized.");
					return;
				}

				this.initialized = true;
				this.ThrowWhenDisposed();

				Log.Debug("Initializing workers for channel group '{0}'.", this.configuration.GroupName);
				this.workers.Initialize(this.TryConnect, this.CanConnect);

				if (!this.DispatchOnly)
				{
					Log.Verbose("Exiting critical section (Initialize)--full-duplex configuration.");
					return;
				}

				Log.Info("Starting dispatch-only worker queue for channel group '{0}'.", this.configuration.GroupName);
				this.workers.StartQueue();
				this.TryOperation(() => { using (this.Connect()) { } });
				Log.Verbose("Exiting critical section (Initialize).");
			}
		}
		protected virtual bool CanConnect()
		{
			using (var channel = this.TryConnect())
				return channel != null;
		}
		protected virtual IMessagingChannel TryConnect()
		{
			Log.Debug("Attempting to establish a channel within channel group '{0}'.", this.configuration.GroupName);

			try
			{
				return this.Connect();
			}
			catch (ChannelConnectionException)
			{
				Log.Debug("The messaging infrastructure for channel group '{0}' is unavailable.", this.configuration.GroupName);
			}
			catch (ObjectDisposedException)
			{
				Log.Debug("Unable to establish a connection for channel group '{0}'; an underlying object has already been disposed.", this.configuration.GroupName);
			}

			return null;
		}
		protected virtual IMessagingChannel Connect()
		{
			this.ThrowWhenUninitialized();
			this.ThrowWhenDisposed();

			return this.connector.Connect(this.configuration.GroupName); // thus causing cancellation and retry
		}

		public virtual IMessagingChannel OpenChannel()
		{
			Log.Debug("Opening a caller-owned channel for group '{0}'.", this.configuration.GroupName);
			return this.Connect();
		}

		public virtual bool BeginDispatch(Action<IDispatchContext> callback)
		{
			if (callback == null)
				throw new ArgumentNullException("callback");

			this.ThrowWhenUninitialized();
			this.ThrowWhenFullDuplex();

			return this.workers.Enqueue(worker => this.TryOperation(() => callback(worker.State.PrepareDispatch())));
		}
		public virtual void BeginReceive(Action<IDeliveryContext> callback)
		{
			if (callback == null)
				throw new ArgumentNullException("callback");

			Log.Info("Beginning receive operation for channel group '{0}'.", this.configuration.GroupName);

			lock (this.sync)
			{
				Log.Verbose("Entering critical section (BeginReceive).");
				this.ThrowWhenDisposed();
				this.ThrowWhenUninitialized();
				this.ThrowWhenAlreadyReceiving();
				this.ThrowWhenDispatchOnly();

				this.receiving = true;
				this.workers.StartActivity(worker => this.TryOperation(() =>
					worker.State.Receive(context => worker.PerformOperation(() => callback(context)))));
				Log.Verbose("Exiting critical section (BeginReceive).");
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
				Log.Debug("Unable to perform operation on channel group '{0}', the connection is unavailable.", this.configuration.GroupName);
				this.TryOperation(this.workers.Restart); // may already be disposed
			}
			catch (ObjectDisposedException)
			{
				// no op
			}
		}

		protected virtual void ThrowWhenDisposed()
		{
			if (!this.disposed)
				return;

			Log.Warn("The channel group has been disposed.");
			throw new ObjectDisposedException(typeof(DefaultChannelGroup).Name);
		}
		protected virtual void ThrowWhenUninitialized()
		{
			if (this.initialized)
				return;

			Log.Warn("The channel group has not been initialized.");
			throw new InvalidOperationException("The channel group has not been initialized.");
		}
		protected virtual void ThrowWhenFullDuplex()
		{
			if (this.configuration.DispatchOnly)
				return;

			Log.Warn("Dispatch can only be performed using a dispatch-only channel group.");
			throw new InvalidOperationException("Dispatch can only be performed using a dispatch-only channel group.");
		}
		protected virtual void ThrowWhenDispatchOnly()
		{
			if (!this.configuration.DispatchOnly)
				return;

			Log.Warn("Dispatch-only channel groups cannot receive messages.");
			throw new InvalidOperationException("Dispatch-only channel groups cannot receive messages.");
		}
		protected virtual void ThrowWhenAlreadyReceiving()
		{
			if (!this.receiving)
				return;

			Log.Warn("A callback has already been provided.");
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

			Log.Verbose("Disposing channel group.");
			lock (this.sync)
			{
				Log.Verbose("Entering critical section (Dispose).");
				if (this.disposed)
				{
					Log.Verbose("Exiting critical section (Dispose)--already disposed.");
					return;
				}

				this.disposed = true;
				this.workers.Dispose();

				Log.Debug("Channel group disposed.");
				Log.Verbose("Exiting critical section (Dispose).");
			}
		}

		private static readonly ILog Log = LogFactory.Build(typeof(DefaultChannelGroup));
		private readonly object sync = new object();
		private readonly IChannelConnector connector;
		private readonly IChannelGroupConfiguration configuration;
		private readonly IWorkerGroup<IMessagingChannel> workers;
		private bool receiving;
		private bool initialized;
		private bool disposed;
	}
}