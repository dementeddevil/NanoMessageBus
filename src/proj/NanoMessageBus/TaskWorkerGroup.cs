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
				throw new ArgumentNullException(nameof(state));

			if (restart == null)
				throw new ArgumentNullException(nameof(restart));

			Log.Debug("Initializing.");

			lock (this._sync)
			{
				Log.Verbose("Entering critical section (Initialize).");
				this.ThrowWhenDisposed();
				this.ThrowWhenInitialized();

				this._initialized = true;
				this._stateCallback = state;
				this._restartCallback = restart;
				Log.Verbose("Exiting critical section (Initialize).");
			}
		}

		public virtual void StartActivity(Action<IWorkItem<T>> activity)
		{
			if (activity == null)
				throw new ArgumentNullException(nameof(activity));

			Log.Debug("Starting worker activity.");
			this.TryStartWorkers((worker, token) => activity(worker));
		}
		protected virtual void TryStartWorkers(Action<IWorkItem<T>, CancellationToken> activity)
		{
			Log.Debug("Attempting to start workers.");

			lock (this._sync)
			{
				Log.Verbose("Entering critical section (TryStartWorkers).");
				this.ThrowWhenDisposed();
				this.ThrowWhenUninitialized();
				this.ThrowWhenAlreadyStarted();

				this._started = true;
				this._tokenSource = new CancellationTokenSource();
				this._activityCallback = activity;
				this._workers.Clear();

				var token = this._tokenSource.Token; // copy on the stack

				Log.Debug("Creating {0} workers.", this._minWorkers);
				for (var i = 0; i < this._minWorkers; i++)
					this._workers.Add(this.StartWorker(() => this.RunActivity(token, activity)));

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
			using (var state = this._stateCallback())
			{
				if (state == null)
				{
					this.Restart();
					return;
				}

				Log.Verbose("Creating worker.");
				var worker = new TaskWorker<T>(state, token, this._minWorkers, this._maxWorkers);

				Log.Verbose("Starting activity.");
				activity(worker, token);
			}
		}

		public virtual bool Enqueue(Action<IWorkItem<T>> workItem)
		{
			if (workItem == null)
				throw new ArgumentNullException(nameof(workItem));

			try
			{
				this.DiscardItem();

				Log.Verbose("Adding new work item.");
				this._workItems.Add(workItem);
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
			if (this._workItems.Count < this._maxQueueSize)
				return;

			Log.Debug("Work item buffer capacity of {0} items reached, discarding earliest work item.",
				this._maxQueueSize);

			Action<IWorkItem<T>> overflow;
			this._workItems.TryTake(out overflow);
		}

		public virtual void StartQueue()
		{
			Log.Debug("Starting work item queue.");
			this.TryStartWorkers(this.WatchQueue);
		}
		protected virtual void WatchQueue(IWorkItem<T> worker, CancellationToken token)
		{
			try
			{
				foreach (var item in this._workItems.GetConsumingEnumerable(token))
					item(worker);
			}
			catch (OperationCanceledException)
			{
				Log.Debug("Token has been canceled; operation canceled.");
			}
		}

		public virtual void Restart()
		{
			Log.Info("Attempting to restart worker group.");

			lock (this._sync)
			{
				Log.Verbose("Entering critical section (Restart).");

				if (this._restarting)
				{
					Log.Verbose("Exiting critical section (Restart)--already restarting.");
					return;
				}

				this._restarting = true;

				this.ThrowWhenDisposed();
				this.ThrowWhenUninitialized();
				this.ThrowWhenNotStarted();

				Log.Verbose("Canceling active workers.");
				this._tokenSource.Cancel(); // GC will perform dispose
				this._tokenSource = new CancellationTokenSource();
				var token = this._tokenSource.Token;

				this._workers.Clear();
				this._workers.Add(this.StartWorker(() => this.Restart(token)));

				Log.Verbose("Exiting critical section (Restart).");
			}
		}
		protected virtual void Restart(CancellationToken token)
		{
			Log.Verbose("Starting single restart worker via the circuit-breaker pattern.");
			while (!token.IsCancellationRequested && !this._restartCallback())
			{
				// FUTURE: sleep for 500ms at a time to check if token has been cancelled
				// as part of that polling, we also check (but at increasing intervals) to
				// determine the activity can be restarted
				Log.Debug("Restart attempt failed, sleeping...");
				this._retrySleepTimeout.Sleep();
			}

			lock (this._sync)
			{
				Log.Verbose("Entering critical section (Restart+CancellationToken).");
				if (this._disposed)
				{
					Log.Debug("Unable to resume activity, worker group has been disposed.");
					Log.Verbose("Exiting critical section (Restart+CancellationToken)--already disposed.");
					return;
				}

				Log.Info("Restart attempt succeeded, shutting down single worker and resuming previous activity.");
				this._started = this._restarting = false;
				this.TryStartWorkers(this._activityCallback);

				Log.Verbose("Exiting critical section (Restart+CancellationToken).");
			}
		}

		protected virtual void ThrowWhenDisposed()
		{
			if (!this._disposed)
				return;

			Log.Warn("The work group has been disposed.");
			throw new ObjectDisposedException(typeof(TaskWorkerGroup<T>).Name);
		}
		protected virtual void ThrowWhenInitialized()
		{
			if (!this._initialized)
				return;

			Log.Warn("The worker group has already been initialized.");
			throw new InvalidOperationException("The work group has already been initialized.");
		}
		protected virtual void ThrowWhenUninitialized()
		{
			if (this._initialized)
				return;

			Log.Warn("The worker group has not been initialized.");
			throw new InvalidOperationException("The work group has not been initialized.");
		}
		protected virtual void ThrowWhenAlreadyStarted()
		{
			if (!this._started)
				return;

			Log.Warn("The worker group has already been started.");
			throw new InvalidOperationException("The worker group has already been started.");
		}
		protected virtual void ThrowWhenNotStarted()
		{
			if (this._started)
				return;

			Log.Warn("The worker group has not yet been started.");
			throw new InvalidOperationException("The worker group has not yet been started.");
		}

		public virtual IEnumerable<Task> Workers
		{
			get { return this._workers; } // for test purposes only
		}

		public TaskWorkerGroup(int minWorkers, int maxWorkers, int maxQueueSize)
		{
			if (minWorkers <= 0)
				throw new ArgumentException("The minimum number of workers is 1.", nameof(minWorkers));

			if (maxWorkers < minWorkers)
				throw new ArgumentException("The maximum number of workers must be at least equal to the minimum number of workers.", nameof(maxWorkers));

			this._minWorkers = minWorkers;
			this._maxWorkers = maxWorkers;
			this._maxQueueSize = maxQueueSize;
		}
		~TaskWorkerGroup()
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

			Log.Verbose("Disposing worker group.");
			lock (this._sync)
			{
				Log.Verbose("Entering critical section (Dispose).");
				if (this._disposed)
				{
					Log.Verbose("Exiting critical section (Dispose)--already disposed.");
					return;
				}

				this._disposed = true;

				this._workItems.CompleteAdding();

				if (this._tokenSource == null)
				{
					Log.Verbose("No active token to be canceled.");
					Log.Verbose("Exiting critical section (Dispose)--no token to dispose.");
					return;
				}

				Log.Verbose("Canceling active token.");
				this._tokenSource.Cancel();
				this._workers.Clear();

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