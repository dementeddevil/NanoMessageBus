namespace NanoMessageBus
{
	using System;
	using System.Collections.Concurrent;
	using System.Threading;
	using System.Threading.Tasks;

	public class TaskWorkerGroup<T> : IWorkerGroup<T>
		where T : class, IDisposable
	{
		public virtual void Initialize(Func<T> state, Func<bool> restart)
		{
			if (state == null)
				throw new ArgumentNullException("state");

			if (restart == null)
				throw new ArgumentNullException("restart");

			lock (this.locker)
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

			this.TryStartWorkers((worker, token) => activity(worker));
		}
		public virtual void StartQueue()
		{
			this.TryStartWorkers((worker, token) =>
			{
				try
				{
					foreach (var item in this.workItems.GetConsumingEnumerable(token))
						item(worker);	
				}
				catch (OperationCanceledException) { }
			});
		}
		protected virtual void TryStartWorkers(Action<IWorkItem<T>, CancellationToken> activity)
		{
			lock (this.locker)
			{
				this.ThrowWhenDisposed();
				this.ThrowWhenUninitialized();
				this.ThrowWhenAlreadyStarted();

				this.activityCallback = activity;
				this.tokenSource = this.tokenSource ?? new CancellationTokenSource();

				for (var i = 0; i < this.minWorkers; i++)
					this.StartWorker(activity);
			}
		}
		protected virtual void StartWorker(Action<IWorkItem<T>, CancellationToken> activity)
		{
			var token = this.tokenSource.Token; // thread-safe copy
			T state = null; // only accessed by a single thread

			Task.Factory
				.StartNew(() => state = this.stateCallback(), TaskCreationOptions.LongRunning)
				.ContinueWith(x => activity(this.CreateWorker(state, token), token))
				.ContinueWith(task => state.TryDispose());
		}
		protected virtual IWorkItem<T> CreateWorker(T state, CancellationToken token)
		{
			return new TaskWorker<T>(state, token, this.minWorkers, this.maxWorkers);
		}

		public virtual void Enqueue(Action<IWorkItem<T>> workItem)
		{
			if (workItem == null)
				throw new ArgumentNullException("workItem");

			this.workItems.Add(workItem);
		}
		public virtual void Restart()
		{
			lock (this.locker)
			{
				this.ThrowWhenDisposed();
				this.ThrowWhenUninitialized();
				this.ThrowWhenNotStarted();

				this.tokenSource.Cancel(); // let the GC cleanup and perform dispose
				var source = this.tokenSource = new CancellationTokenSource();
				this.StartWorker((worker, token) =>
				{
					while (!source.Token.IsCancellationRequested && !this.restartCallback())
						RetrySleepTimeout.Sleep();

					this.tokenSource.Dispose();
					this.tokenSource = null;

					if (!this.disposed)
						this.TryStartWorkers(this.activityCallback);
				});
			}
		}

		protected virtual void ThrowWhenDisposed()
		{
			if (this.disposed)
				throw new ObjectDisposedException(typeof(DefaultMessagingHost).Name);
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

			lock (this.locker)
			{
				if (this.disposed)
					return;

				this.disposed = true;

				if (this.tokenSource != null)
					this.tokenSource.Cancel(); // GC will perform dispose
			}
		}

		private static readonly TimeSpan RetrySleepTimeout = TimeSpan.FromMilliseconds(2500); // 2.5 seconds
		private readonly object locker = new object();
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