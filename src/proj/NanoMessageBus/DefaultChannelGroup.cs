using System.Threading.Tasks;

namespace NanoMessageBus
{
	using System;
	using Logging;

	public class DefaultChannelGroup : IChannelGroup
	{
		public virtual bool DispatchOnly => _configuration.DispatchOnly;

	    public virtual void Initialize()
		{
			Log.Info("Initializing channel group '{0}'.", _configuration.GroupName);

			lock (_sync)
			{
				Log.Verbose("Entering critical section (Initialize).");
				if (_initialized)
				{
					Log.Verbose("Exiting critical section (Initialize)--already initialized.");
					return;
				}

				_initialized = true;
				ThrowWhenDisposed();

				Log.Debug("Initializing workers for channel group '{0}'.", _configuration.GroupName);
				_workers.Initialize(TryConnect, CanConnect);

				if (!DispatchOnly)
				{
					Log.Verbose("Exiting critical section (Initialize)--full-duplex configuration.");
					return;
				}

				Log.Info("Starting dispatch-only worker queue for channel group '{0}'.", _configuration.GroupName);
				_workers.StartQueue();
				TryOperation(() => { using (Connect()) { } });
				Log.Verbose("Exiting critical section (Initialize).");
			}
		}
		protected virtual bool CanConnect()
		{
			using (var channel = TryConnect())
				return channel != null;
		}
		protected virtual IMessagingChannel TryConnect()
		{
			Log.Debug("Attempting to establish a channel within channel group '{0}'.", _configuration.GroupName);

			try
			{
				return Connect();
			}
			catch (ChannelConnectionException)
			{
				Log.Debug("The messaging infrastructure for channel group '{0}' is unavailable.", _configuration.GroupName);
			}
			catch (ObjectDisposedException)
			{
				Log.Debug("Unable to establish a connection for channel group '{0}'; an underlying object has already been disposed.", _configuration.GroupName);
			}

			return null;
		}
		protected virtual IMessagingChannel Connect()
		{
			ThrowWhenUninitialized();
			ThrowWhenDisposed();

			return _connector.Connect(_configuration.GroupName); // thus causing cancellation and retry
		}

		public virtual IMessagingChannel OpenChannel()
		{
			Log.Debug("Opening a caller-owned channel for group '{0}'.", _configuration.GroupName);
			return Connect();
		}

		public virtual bool BeginDispatch(Action<IDispatchContext> callback)
		{
			if (callback == null)
			{
			    throw new ArgumentNullException(nameof(callback));
			}

		    ThrowWhenUninitialized();
			ThrowWhenFullDuplex();

			return _workers.Enqueue(worker => TryBeginDispatch(worker, callback));
		}
		protected virtual void TryBeginDispatch(IWorkItem<IMessagingChannel> worker, Action<IDispatchContext> callback)
		{
			TryOperation(() =>
			{
				try
				{
					callback(worker.State.PrepareDispatch());
				}
				catch (ChannelConnectionException)
				{
					Log.Debug("Work item failed due to lost connection; re-enqueuing for later attempt.");
					BeginDispatch(callback);
					throw;
				}
			});
		}

		public virtual void BeginReceive(Func<IDeliveryContext, Task> callback)
		{
			if (callback == null)
			{
			    throw new ArgumentNullException(nameof(callback));
			}

		    Log.Info("Beginning receive operation for channel group '{0}'.", _configuration.GroupName);

			lock (_sync)
			{
				Log.Verbose("Entering critical section (BeginReceive).");
				ThrowWhenDisposed();
				ThrowWhenUninitialized();
				ThrowWhenAlreadyReceiving();
				ThrowWhenDispatchOnly();

				_receiving = true;
				_workers.StartActivity(
                    worker => TryOperation(
                        () => worker.State.ReceiveAsync(
                            context => worker.PerformOperation(
                                () => callback(context)))));

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
				Log.Debug("Unable to perform operation on channel group '{0}', the connection is unavailable.", _configuration.GroupName);
				TryOperation(_workers.Restart); // may already be disposed
			}
			catch (ObjectDisposedException)
			{
				// no op
			}
		}

		protected virtual void ThrowWhenDisposed()
		{
			if (!_disposed)
			{
			    return;
			}

		    Log.Warn("The channel group has been disposed.");
			throw new ObjectDisposedException(typeof(DefaultChannelGroup).Name);
		}
		protected virtual void ThrowWhenUninitialized()
		{
			if (_initialized)
			{
			    return;
			}

		    Log.Warn("The channel group has not been initialized.");
			throw new InvalidOperationException("The channel group has not been initialized.");
		}
		protected virtual void ThrowWhenFullDuplex()
		{
			if (_configuration.DispatchOnly)
			{
			    return;
			}

		    Log.Warn("Dispatch can only be performed using a dispatch-only channel group.");
			throw new InvalidOperationException("Dispatch can only be performed using a dispatch-only channel group.");
		}
		protected virtual void ThrowWhenDispatchOnly()
		{
			if (!_configuration.DispatchOnly)
			{
			    return;
			}

		    Log.Warn("Dispatch-only channel groups cannot receive messages.");
			throw new InvalidOperationException("Dispatch-only channel groups cannot receive messages.");
		}
		protected virtual void ThrowWhenAlreadyReceiving()
		{
			if (!_receiving)
			{
			    return;
			}

		    Log.Warn("A callback has already been provided.");
			throw new InvalidOperationException("A callback has already been provided.");
		}

		public DefaultChannelGroup(
			IChannelConnector connector, IChannelGroupConfiguration configuration, IWorkerGroup<IMessagingChannel> workers)
		{
			_connector = connector;
			_configuration = configuration;
			_workers = workers;
		}
		~DefaultChannelGroup()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
			{
			    return;
			}

		    Log.Verbose("Disposing channel group.");
			lock (_sync)
			{
				Log.Verbose("Entering critical section (Dispose).");
				if (_disposed)
				{
					Log.Verbose("Exiting critical section (Dispose)--already disposed.");
					return;
				}

				_disposed = true;
				_workers.TryDispose();

				Log.Info("Channel group disposed.");
				Log.Verbose("Exiting critical section (Dispose).");
			}
		}

		private static readonly ILog Log = LogFactory.Build(typeof(DefaultChannelGroup));
		private readonly object _sync = new object();
		private readonly IChannelConnector _connector;
		private readonly IChannelGroupConfiguration _configuration;
		private readonly IWorkerGroup<IMessagingChannel> _workers;
		private bool _receiving;
		private bool _initialized;
		private bool _disposed;
	}
}