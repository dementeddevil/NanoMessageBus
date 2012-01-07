namespace NanoMessageBus
{
	using System;
	using System.Collections.Concurrent;
	using System.Threading;

	public class TaskWorkerGroup<TState> : IWorkerGroup<TState>
		where TState : IDisposable
	{
		public virtual void Initialize(Func<TState> state, Func<bool> restart)
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

		public virtual void StartActivity(Action<TState> activity)
		{
			if (activity == null)
				throw new ArgumentNullException("activity");

			// TODO: startup minWorkers that perform the activity provided
			this.TryStart(() => this.activityCallback = activity);
		}
		public virtual void StartQueue()
		{
			// TODO: startup minWorkers that watch the workItem queue
			this.TryStart(() => { });
		}
		protected virtual void TryStart(Action callback)
		{
			lock (this.locker)
			{
				this.ThrowWhenDisposed();
				this.ThrowWhenUninitialized();
				this.ThrowWhenAlreadyStarted();

				this.started = true;
				callback();
			}
		}

		public virtual void Enqueue(Action<TState> workItem)
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

				// TODO:
				// 1. prepare all workers and their corresponding TState to be cleanly and safely disposed
				//    (perhaps disposal is the last part of the task?)
				// 2. clear the workers collection
				// 3. startup a single task and TState that invokes the restart callback
				// 4. success brings everything back online
				// 5. failure continues but with a longer thread on the task thread
				//    make sure we check dispose at each loop
				//    do we need some kind of 1-second thread sleep that wakes up to ensure dispose hasn't been called
				//    and to allow/facilitate clean shutdown of the entire app domain?
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
			if (this.started)
				throw new InvalidOperationException("The worker group has already been started.");
		}
		protected virtual void ThrowWhenNotStarted()
		{
			if (!this.started)
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

				// TODO: clean shutdown
				this.disposed = true;
			}
		}

		private readonly object locker = new object();
		private readonly int minWorkers;
		private readonly int maxWorkers;

		private readonly BlockingCollection<Action<TState>> workItems = new BlockingCollection<Action<TState>>();

		private CancellationTokenSource tokenSource;

		private Action<TState> activityCallback;
		private Func<TState> stateCallback;
		private Func<bool> restartCallback;

		private bool initialized;
		private bool started;
		private bool disposed;
	}
}