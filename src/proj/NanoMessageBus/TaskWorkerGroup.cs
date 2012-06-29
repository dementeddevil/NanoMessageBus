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
				throw new ArgumentNullException("state");

			if (restart == null)
				throw new ArgumentNullException("restart");

			Log.Debug("Initializing.");

			lock (this.sync)
			{
				Log.Verbose("Entering critical section (Initialize).");
				this.ThrowWhenDisposed();
				this.ThrowWhenInitialized();

				this.initialized = true;
				this.stateCallback = state;
				this.restartCallback = restart;
				Log.Verbose("Exiting critical section (Initialize).");
			}
		}

		public virtual void StartActivity(Action<IWorkItem<T>> activity)
		{
			if (activity == null)
				throw new ArgumentNullException("activity");

			Log.Debug("Starting worker activity.");
			this.TryStartWorkers((worker, token) => activity(worker));
		}
		protected virtual void TryStartWorkers(Action<IWorkItem<T>, CancellationToken> activity)
		{
			Log.Debug("Attempting to start workers.");

			lock (this.sync)
			{
				Log.Verbose("Entering critical section (TryStartWorkers).");
				this.ThrowWhenDisposed();
				this.ThrowWhenUninitialized();
				this.ThrowWhenAlreadyStarted();

				this.started = true;
				this.tokenSource = new CancellationTokenSource();
				this.activityCallback = activity;
				this.workers.Clear();

				var token = this.tokenSource.Token; // copy on the stack

				Log.Debug("Creating {0} workers.", this.minWorkers);
				for (var i = 0; i < this.minWorkers; i++)
					this.workers.Add(this.StartWorker(() => this.RunActivity(token, activity)));

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
			using (var state = this.stateCallback())
			{
				if (state == null)
				{
					this.Restart();
					return;
				}

				Log.Verbose("Creating worker.");
				var worker = new TaskWorker<T>(state, token, this.minWorkers, this.maxWorkers);

				Log.Verbose("Starting activity.");
				activity(worker, token);
			}
		}

		public virtual bool Enqueue(Action<IWorkItem<T>> workItem)
		{
			if (workItem == null)
				throw new ArgumentNullException("workItem");

			try
			{
				this.DiscardItem();

				Log.Verbose("Adding new work item.");
				this.workItems.Add(workItem);
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
			if (this.workItems.Count < this.maxQueueSize)
				return;

			Log.Debug("Work item buffer capacity of {0} items reached, discarding earliest work item.",
				this.maxQueueSize);

			Action<IWorkItem<T>> overflow;
			this.workItems.TryTake(out overflow);
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
				foreach (var item in this.workItems.GetConsumingEnumerable(token))
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

			lock (this.sync)
			{
				Log.Verbose("Entering critical section (Restart).");

				if (this.restarting)
				{
					Log.Verbose("Exiting critical section (Restart)--already restarting.");
					return;
				}

				this.restarting = true;

				this.ThrowWhenDisposed();
				this.ThrowWhenUninitialized();
				this.ThrowWhenNotStarted();

				Log.Verbose("Canceling active workers.");
				this.tokenSource.Cancel(); // GC will perform dispose
				this.tokenSource = new CancellationTokenSource();
				var token = this.tokenSource.Token;

				this.workers.Clear();
				this.workers.Add(this.StartWorker(() => this.Restart(token)));

				Log.Verbose("Exiting critical section (Restart).");
			}
		}
		protected virtual void Restart(CancellationToken token)
		{
			Log.Verbose("Starting single restart worker à-la circuit-breaker pattern.");
			while (!token.IsCancellationRequested && !this.restartCallback())
			{
				// FUTURE: sleep for 500ms at a time to check if token has been cancelled
				// as part of that polling, we also check (but at increasing intervals) to
				// determine the activity can be restarted
				Log.Debug("Restart attempt failed, sleeping...");
				this.retrySleepTimeout.Sleep();
			}

			lock (this.sync)
			{
				Log.Verbose("Entering critical section (Restart+CancellationToken).");
				if (this.disposed)
				{
					Log.Debug("Unable to resume activity, worker group has been disposed.");
					Log.Verbose("Exiting critical section (Restart+CancellationToken)--already disposed.");
					return;
				}

				Log.Info("Restart attempt succeeded, shutting down single worker and resuming previous activity.");
				this.started = this.restarting = false;
				this.TryStartWorkers(this.activityCallback);

				Log.Verbose("Exiting critical section (Restart+CancellationToken).");
			}
		}

		protected virtual void ThrowWhenDisposed()
		{
			if (!this.disposed)
				return;

			Log.Warn("The work group has been disposed.");
			throw new ObjectDisposedException(typeof(TaskWorkerGroup<T>).Name);
		}
		protected virtual void ThrowWhenInitialized()
		{
			if (!this.initialized)
				return;

			Log.Warn("The worker group has already been initialized.");
			throw new InvalidOperationException("The work group has already been initialized.");
		}
		protected virtual void ThrowWhenUninitialized()
		{
			if (this.initialized)
				return;

			Log.Warn("The worker group has not been initialized.");
			throw new InvalidOperationException("The work group has not been initialized.");
		}
		protected virtual void ThrowWhenAlreadyStarted()
		{
			if (!this.started)
				return;

			Log.Warn("The worker group has already been started.");
			throw new InvalidOperationException("The worker group has already been started.");
		}
		protected virtual void ThrowWhenNotStarted()
		{
			if (this.started)
				return;

			Log.Warn("The worker group has not yet been started.");
			throw new InvalidOperationException("The worker group has not yet been started.");
		}

		public virtual IEnumerable<Task> Workers
		{
			get { return this.workers; } // for test purposes only
		}

		public TaskWorkerGroup(int minWorkers, int maxWorkers, int maxQueueSize)
		{
			if (minWorkers <= 0)
				throw new ArgumentException("The minimum number of workers is 1.", "minWorkers");

			if (maxWorkers < minWorkers)
				throw new ArgumentException("The maximum number of workers must be greater than the minimum number of workers.", "maxWorkers");

			this.minWorkers = minWorkers;
			this.maxWorkers = maxWorkers;
			this.maxQueueSize = maxQueueSize;
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
			lock (this.sync)
			{
				Log.Verbose("Entering critical section (Dispose).");
				if (this.disposed)
				{
					Log.Verbose("Exiting critical section (Dispose)--already disposed.");
					return;
				}

				this.disposed = true;

				this.workItems.CompleteAdding();

				if (this.tokenSource == null)
				{
					Log.Verbose("No active token to be canceled.");
					Log.Verbose("Exiting critical section (Dispose)--no token to dispose.");
					return;
				}

				Log.Verbose("Canceling active token.");
				this.tokenSource.Cancel();
				this.workers.Clear();

				Log.Debug("Worker group disposed.");
				Log.Verbose("Exiting critical section (Dispose).");
			}
		}

		private static readonly ILog Log = LogFactory.Build(typeof(TaskWorkerGroup<>));
		private readonly TimeSpan retrySleepTimeout = TimeSpan.FromMilliseconds(2500); // 2.5 seconds
		private readonly object sync = new object();
		private readonly BlockingCollection<Action<IWorkItem<T>>> workItems = new BlockingCollection<Action<IWorkItem<T>>>();
		private readonly ICollection<Task> workers = new LinkedList<Task>();
		private readonly int minWorkers;
		private readonly int maxWorkers;
		private readonly int maxQueueSize;
		private CancellationTokenSource tokenSource;
		private Action<IWorkItem<T>, CancellationToken> activityCallback;
		private Func<T> stateCallback;
		private Func<bool> restartCallback;
		private bool initialized;
		private bool started;
		private bool disposed;
		private bool restarting;
	}
}