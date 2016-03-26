namespace NanoMessageBus
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;
	using Logging;

	public class TaskWorkerGroup<T> : IWorkerGroup<T>
		where T : class, IDisposable
	{
		public virtual void Initialize(Func<T> state, Func<bool> restart)
		{
			if (state == null)
			{
			    throw new ArgumentNullException(nameof(state));
			}

		    if (restart == null)
		    {
		        throw new ArgumentNullException(nameof(restart));
		    }

		    Log.Debug("Initializing.");

			lock (_sync)
			{
				Log.Verbose("Entering critical section (Initialize).");
				ThrowWhenDisposed();
				ThrowWhenInitialized();

				_initialized = true;
				_stateCallback = state;
				_restartCallback = restart;
				Log.Verbose("Exiting critical section (Initialize).");
			}
		}

		public virtual void StartActivity(Action<IWorkItem<T>> activity)
		{
			if (activity == null)
			{
			    throw new ArgumentNullException(nameof(activity));
			}

		    Log.Debug("Starting worker activity.");
			TryStartWorkers((worker, token) => activity(worker));
		}

		protected virtual void TryStartWorkers(Action<IWorkItem<T>, CancellationToken> activity)
		{
			Log.Debug("Attempting to start workers.");

			lock (_sync)
			{
				Log.Verbose("Entering critical section (TryStartWorkers).");
				ThrowWhenDisposed();
				ThrowWhenUninitialized();
				ThrowWhenAlreadyStarted();

				_started = true;
				_tokenSource = new CancellationTokenSource();
				_activityCallback = activity;
				_workers.Clear();

				var token = _tokenSource.Token; // copy on the stack

				Log.Debug("Creating {0} workers.", _minWorkers);
				for (var i = 0; i < _minWorkers; i++)
				{
				    _workers.Add(StartWorker(() => RunActivity(token, activity)));
				}

			    Log.Verbose("Exiting critical section (TryStartWorkers).");
			}
		}
		protected virtual Task StartWorker(Action activity)
		{
			Log.Verbose("Starting worker.");
			return Task.Factory.StartNew(activity, TaskCreationOptions.LongRunning);
		}

		protected virtual void RunActivity(CancellationToken token, Action<IWorkItem<T>, CancellationToken> activity)
		{
			using (var state = _stateCallback())
			{
				if (state == null)
				{
					Restart();
					return;
				}

				Log.Verbose("Creating worker.");
				var worker = new TaskWorker<T>(state, token, _minWorkers, _maxWorkers);

				Log.Verbose("Starting activity.");
				activity(worker, token);
			}
		}

		public virtual bool Enqueue(Action<IWorkItem<T>> workItem)
		{
			if (workItem == null)
			{
			    throw new ArgumentNullException(nameof(workItem));
			}

		    try
			{
				DiscardItem();

				Log.Verbose("Adding new work item.");
				_workItems.Add(workItem);
				return true;
			}
			catch (InvalidOperationException)
			{
				Log.Debug("Workers no longer accepting additional items.");
				return false;
			}
		}

		protected virtual void DiscardItem()
		{
			if (_workItems.Count < _maxQueueSize)
			{
			    return;
			}

		    Log.Debug("Work item buffer capacity of {0} items reached, discarding earliest work item.",
				_maxQueueSize);

			Action<IWorkItem<T>> overflow;
			_workItems.TryTake(out overflow);
		}

		public virtual void StartQueue()
		{
			Log.Debug("Starting work item queue.");
			TryStartWorkers(WatchQueue);
		}

		protected virtual void WatchQueue(IWorkItem<T> worker, CancellationToken token)
		{
			try
			{
				foreach (var item in _workItems.GetConsumingEnumerable(token))
				{
				    item(worker);
				}
			}
			catch (OperationCanceledException)
			{
				Log.Debug("Token has been canceled; operation canceled.");
			}
		}

		public virtual void Restart()
		{
			Log.Info("Attempting to restart worker group.");

			lock (_sync)
			{
				Log.Verbose("Entering critical section (Restart).");

				if (_restarting)
				{
					Log.Verbose("Exiting critical section (Restart)--already restarting.");
					return;
				}

				_restarting = true;

				ThrowWhenDisposed();
				ThrowWhenUninitialized();
				ThrowWhenNotStarted();

				Log.Verbose("Canceling active workers.");
				_tokenSource.Cancel(); // GC will perform dispose
				_tokenSource = new CancellationTokenSource();
				var token = _tokenSource.Token;

				_workers.Clear();
				_workers.Add(StartWorker(() => Restart(token)));

				Log.Verbose("Exiting critical section (Restart).");
			}
		}

		protected virtual void Restart(CancellationToken token)
		{
			Log.Verbose("Starting single restart worker via the circuit-breaker pattern.");
			while (!token.IsCancellationRequested && !_restartCallback())
			{
				// FUTURE: sleep for 500ms at a time to check if token has been cancelled
				// as part of that polling, we also check (but at increasing intervals) to
				// determine the activity can be restarted
				Log.Debug("Restart attempt failed, sleeping...");
				_retrySleepTimeout.Sleep();
			}

			lock (_sync)
			{
				Log.Verbose("Entering critical section (Restart+CancellationToken).");
				if (_disposed)
				{
					Log.Debug("Unable to resume activity, worker group has been disposed.");
					Log.Verbose("Exiting critical section (Restart+CancellationToken)--already disposed.");
					return;
				}

				Log.Info("Restart attempt succeeded, shutting down single worker and resuming previous activity.");
				_started = _restarting = false;
				TryStartWorkers(_activityCallback);

				Log.Verbose("Exiting critical section (Restart+CancellationToken).");
			}
		}

		protected virtual void ThrowWhenDisposed()
		{
			if (!_disposed)
			{
			    return;
			}

		    Log.Warn("The work group has been disposed.");
			throw new ObjectDisposedException(typeof(TaskWorkerGroup<T>).Name);
		}

		protected virtual void ThrowWhenInitialized()
		{
			if (!_initialized)
			{
			    return;
			}

		    Log.Warn("The worker group has already been initialized.");
			throw new InvalidOperationException("The work group has already been initialized.");
		}

		protected virtual void ThrowWhenUninitialized()
		{
			if (_initialized)
			{
			    return;
			}

		    Log.Warn("The worker group has not been initialized.");
			throw new InvalidOperationException("The work group has not been initialized.");
		}

		protected virtual void ThrowWhenAlreadyStarted()
		{
			if (!_started)
			{
			    return;
			}

		    Log.Warn("The worker group has already been started.");
			throw new InvalidOperationException("The worker group has already been started.");
		}

		protected virtual void ThrowWhenNotStarted()
		{
			if (_started)
			{
			    return;
			}

		    Log.Warn("The worker group has not yet been started.");
			throw new InvalidOperationException("The worker group has not yet been started.");
		}

		public virtual IEnumerable<Task> Workers => _workers;

	    public TaskWorkerGroup(int minWorkers, int maxWorkers, int maxQueueSize)
		{
			if (minWorkers <= 0)
			{
			    throw new ArgumentException("The minimum number of workers is 1.", nameof(minWorkers));
			}

	        if (maxWorkers < minWorkers)
	        {
	            throw new ArgumentException("The maximum number of workers must be at least equal to the minimum number of workers.", nameof(maxWorkers));
	        }

	        _minWorkers = minWorkers;
			_maxWorkers = maxWorkers;
			_maxQueueSize = maxQueueSize;
		}
		~TaskWorkerGroup()
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

		    Log.Verbose("Disposing worker group.");
			lock (_sync)
			{
				Log.Verbose("Entering critical section (Dispose).");
				if (_disposed)
				{
					Log.Verbose("Exiting critical section (Dispose)--already disposed.");
					return;
				}

				_disposed = true;

				_workItems.CompleteAdding();

				if (_tokenSource == null)
				{
					Log.Verbose("No active token to be canceled.");
					Log.Verbose("Exiting critical section (Dispose)--no token to dispose.");
					return;
				}

				Log.Verbose("Canceling active token.");
				_tokenSource.Cancel();
				_workers.Clear();

				Log.Debug("Worker group disposed.");
				Log.Verbose("Exiting critical section (Dispose).");
			}
		}

// ReSharper disable StaticFieldInGenericType
		private static readonly ILog Log = LogFactory.Build(typeof(TaskWorkerGroup<>));
// ReSharper restore StaticFieldInGenericType
		private readonly TimeSpan _retrySleepTimeout = TimeSpan.FromMilliseconds(2500); // 2.5 seconds
		private readonly object _sync = new object();
		private readonly BlockingCollection<Action<IWorkItem<T>>> _workItems = new BlockingCollection<Action<IWorkItem<T>>>();
		private readonly ICollection<Task> _workers = new LinkedList<Task>();
		private readonly int _minWorkers;
		private readonly int _maxWorkers;
		private readonly int _maxQueueSize;
		private CancellationTokenSource _tokenSource;
		private Action<IWorkItem<T>, CancellationToken> _activityCallback;
		private Func<T> _stateCallback;
		private Func<bool> _restartCallback;
		private bool _initialized;
		private bool _started;
		private bool _disposed;
		private bool _restarting;
	}
}