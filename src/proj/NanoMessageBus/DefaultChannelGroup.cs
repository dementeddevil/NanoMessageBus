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
				if (this.initialized)
					return;

				this.initialized = true;
				this.ThrowWhenDisposed();

				// TODO: it appears that if the infrastructure is down at startup, things never start working if/when it comes online
				Log.Debug("Initializing workers for channel group '{0}'.", this.configuration.GroupName);
				this.workers.Initialize(() =>
				{
					// TODO: get this under test: connecting *may* throw an exception (thus causing the restart activity)
					// which then returns a null channel here
					IMessagingChannel channel = null;
					this.TryOperation(() =>
						channel = this.Connect());
					return channel;
				}, this.TryConnect);

				if (!this.TryConnect() || !this.DispatchOnly)
					return;

				Log.Info("Connected. Starting dispatch-only worker queue for channel group '{0}'.", this.configuration.GroupName);
				this.workers.StartQueue();
			}
		}
		protected virtual IMessagingChannel Connect()
		{
			Log.Debug("Attempting to establish a channel within channel group '{0}'.", this.configuration.GroupName);

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
				Log.Debug("The messaging infrastructure for channel group '{0}' is unavailable.", this.configuration.GroupName);
				return false;
			}
			catch (ObjectDisposedException)
			{
				Log.Debug("Unable to establish a connection for channel group '{0}'; an underlying object has already been disposed.", this.configuration.GroupName);
				return false;
			}
		}

		public virtual void BeginDispatch(ChannelEnvelope envelope, Action<IChannelTransaction> completed)
		{
			if (envelope == null)
				throw new ArgumentNullException("envelope");

			if (completed == null)
				throw new ArgumentNullException("completed");

			this.ThrowWhenDisposed(); // TODO: external threads can hit this and throw because this CG is being shutdown.
			this.ThrowWhenUninitialized();
			this.ThrowWhenFullDuplex();

			Log.Verbose("Adding message '{0}' to dispatch queue for channel group '{0}'.", envelope.Message.MessageId, this.configuration.GroupName);
			this.workers.Enqueue(worker => this.TryOperation(() =>
			{
				Log.Verbose("Pushing message '{0}' into the channel for dispatch for channel group '{0}'.", envelope.Message.MessageId, this.configuration.GroupName);

				var channel = worker.State;
				channel.Send(envelope);
				completed(channel.CurrentTransaction);
			}));
		}
		public virtual void BeginReceive(Action<IDeliveryContext> callback)
		{
			if (callback == null)
				throw new ArgumentNullException("callback");

			Log.Info("Beginning receive operation for channel group '{0}'.", this.configuration.GroupName);

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
			if (this.disposed)
				throw new ObjectDisposedException(typeof(DefaultChannelGroup).Name);
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

				Log.Debug("Shutting down channel group.");
				this.disposed = true;
				this.workers.Dispose();
			}
		}

		private static readonly ILog Log = LogFactory.Builder(typeof(DefaultChannelGroup));
		private readonly object sync = new object();
		private readonly IChannelConnector connector;
		private readonly IChannelGroupConfiguration configuration;
		private readonly IWorkerGroup<IMessagingChannel> workers;
		private bool receiving;
		private bool initialized;
		private bool disposed;
	}
}