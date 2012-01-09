namespace NanoMessageBus
{
	using System;
	using System.Collections.Concurrent;
	using System.Threading;
	using System.Threading.Tasks;

	public class TaskWorkerGroup<TState> : IWorkerGroup<TState>
		where TState : class, IDisposable
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

		public virtual void StartActivity(Action<TState> activity) // TODO: cancellation callback needed to inform activity of cancellation token
		{
			if (activity == null)
				throw new ArgumentNullException("activity");

			this.TryStart(() =>
			{
				this.activityCallback = activity;
				var token = this.tokenSource.Token; // copy of token within protected/locked boundary for later use

				for (var i = 0; i < this.minWorkers; i++)
				{
					TState state = null;

					Task.Factory
						.StartNew(
							() =>
							{
								// TODO: register cancellation callback with token
								state = this.stateCallback();
								this.activityCallback(state); // TODO: how is the activity aware of cancellation?
							},
							TaskCreationOptions.LongRunning)
						.ContinueWith(task => state.Dispose());
				}
			});
		}
		public virtual void StartQueue()
		{
			this.TryStart(() =>
			{
				var token = this.tokenSource.Token; // copy of token within protected/locked boundary for later use

				for (var i = 0; i < this.minWorkers; i++)
				{
					TState state = null;

					Task.Factory
						.StartNew(
							() =>
							{
						      	state = this.stateCallback();
								this.StartQueueTask(state, token);
							},
							TaskCreationOptions.LongRunning)
						.ContinueWith(task => state.Dispose());
				}
			});
		}
		protected void StartQueueTask(TState state, CancellationToken token)
		{
			foreach (var item in this.workItems.GetConsumingEnumerable(token))
				item(state);
		}

		protected virtual void TryStart(Action callback)
		{
			lock (this.locker)
			{
				this.ThrowWhenDisposed();
				this.ThrowWhenUninitialized();
				this.ThrowWhenAlreadyStarted();

				this.tokenSource = new CancellationTokenSource();
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

				// TODO: cancel the token and start the polling task on a single thread with the restartCallback
				// be aware of sleeping for too long (longer than 1-2 seconds) between
				// checks to the cancellation token
				// success nullifies the current token (in a lock statement) and
				// then invokes the previously running activity
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
					this.tokenSource.Cancel();
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
		private bool disposed;
	}
}