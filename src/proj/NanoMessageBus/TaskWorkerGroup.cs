namespace NanoMessageBus
{
	using System;
	using System.Collections.Concurrent;
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
				this.ThrowWhenDisposed();
				this.ThrowWhenInitialized();

				this.initialized = true;
				this.stateCallback = state;
				this.restartCallback = restart;
			}
		}

		public virtual void StartActivity(Action<IWorkItem<T>> activity)
		{
			if (activity == null)
				throw new ArgumentNullException("activity");

			Log.Debug("Starting activity.");

			this.TryStartWorkers((worker, token) => activity(worker));
		}
		public virtual void StartQueue()
		{
			Log.Debug("Starting queue.");

			this.TryStartWorkers((worker, token) =>
			{
				try
				{
					foreach (var item in this.workItems.GetConsumingEnumerable(token))
						item(worker);
				}
				catch (OperationCanceledException)
				{
					Log.Debug("Token has been canceled, operation canceled.");
				}
			});
		}
		protected virtual void TryStartWorkers(Action<IWorkItem<T>, CancellationToken> activity)
		{
			Log.Debug("Attempting to start workers.");

			lock (this.sync)
			{
				this.ThrowWhenDisposed();
				this.ThrowWhenUninitialized();
				this.ThrowWhenAlreadyStarted();

				this.activityCallback = activity;
				this.tokenSource = this.tokenSource ?? new CancellationTokenSource();

				Log.Debug("Creating {0} workers.", this.minWorkers);
				for (var i = 0; i < this.minWorkers; i++)
					this.StartWorker(activity);
			}
		}
		protected virtual void StartWorker(Action<IWorkItem<T>, CancellationToken> activity)
		{
			Log.Verbose("Starting worker.");
			var token = this.tokenSource.Token; // thread-safe copy

			Task.Factory.StartNew(() =>
			{
				using (var state = this.stateCallback())
				{
					if (state == null)
						return;

					Log.Verbose("Creating worker.");
					var worker = new TaskWorker<T>(state, token, this.minWorkers, this.maxWorkers);

					Log.Verbose("Starting activity.");
					activity(worker, token);
				}
			}, TaskCreationOptions.LongRunning);
		}

		public virtual void Enqueue(Action<IWorkItem<T>> workItem)
		{
			if (workItem == null)
				throw new ArgumentNullException("workItem");

			Log.Verbose("Adding work item.");
			this.workItems.Add(workItem);
		}
		public virtual void Restart()
		{
			// TODO: if a restart attempt is underway, the thread making the duplicate call should exit this method without performing any action
			Log.Info("Attempting to restart worker group.");

			lock (this.sync)
			{
				this.ThrowWhenDisposed();
				this.ThrowWhenUninitialized();
				this.ThrowWhenNotStarted();

				Log.Verbose("Canceling token to allow cleanup.");

				this.tokenSource.Cancel(); // let the GC cleanup and perform dispose
				var source = this.tokenSource = new CancellationTokenSource();
				this.StartWorker((worker, token) =>
				{
					Log.Verbose("Starting single worker à la circuit-breaker pattern.");
					while (!source.Token.IsCancellationRequested && !this.restartCallback())
					{
						Log.Debug("Restart attempt failed, sleeping...");
						this.retrySleepTimeout.Sleep();
					}

					Log.Debug("Restart attempt succeeded, shutting down single worker.");
					this.tokenSource.Dispose();
					this.tokenSource = null;

					if (this.disposed)
						return;

					Log.Debug("Restart attempt succeeded, starting main activity.");
					this.TryStartWorkers(this.activityCallback);
				});
			}
		}

		protected virtual void ThrowWhenDisposed()
		{
			if (this.disposed)
				throw new ObjectDisposedException(typeof(TaskWorkerGroup<T>).Name);
		}
		protected virtual void ThrowWhenInitialized()
		{
			if (this.initialized)
				throw new InvalidOperationException("The host has already been initialized.");
		}
		protected virtual void ThrowWhenUninitialized()
		{
			if (!this.initialized)
				throw new InvalidOperationException("The host has not been initialized.");
		}
		protected virtual void ThrowWhenAlreadyStarted()
		{
			if (this.tokenSource != null)
				throw new InvalidOperationException("The worker group has already been started.");
		}
		protected virtual void ThrowWhenNotStarted()
		{
			if (this.tokenSource == null)
				throw new InvalidOperationException("The worker group has not yet been started.");
		}

		public TaskWorkerGroup(int minWorkers, int maxWorkers)
		{
			if (minWorkers <= 0)
				throw new ArgumentException("The minimum number of workers is 1.", "minWorkers");

			if (maxWorkers < minWorkers)
				throw new ArgumentException("The maximum number of workers must be greater than the minimum number of workers.", "maxWorkers");

			this.minWorkers = minWorkers;
			this.maxWorkers = maxWorkers;
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

			lock (this.sync)
			{
				if (this.disposed)
					return;

				this.disposed = true;

				Log.Debug("Shutting down worker group.");
				if (this.tokenSource == null)
					return;

				// TODO: call dispose on this.workItems, but add test to verify that ObjectDisposedException is handled.
				Log.Verbose("Canceling token.");
				this.tokenSource.Cancel(); // GC will perform dispose
			}
		}

		private static readonly ILog Log = LogFactory.Builder(typeof(TaskWorkerGroup<T>));
		private readonly TimeSpan retrySleepTimeout = TimeSpan.FromMilliseconds(2500); // 2.5 seconds
		private readonly object sync = new object();
		private readonly BlockingCollection<Action<IWorkItem<T>>> workItems = new BlockingCollection<Action<IWorkItem<T>>>();
		private readonly int minWorkers;
		private readonly int maxWorkers;
		private CancellationTokenSource tokenSource;
		private Action<IWorkItem<T>, CancellationToken> activityCallback;
		private Func<T> stateCallback;
		private Func<bool> restartCallback;
		private bool initialized;
		private bool disposed;
	}
}