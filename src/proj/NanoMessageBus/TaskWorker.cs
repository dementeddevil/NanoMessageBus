namespace NanoMessageBus
{
	using System;
	using System.Threading;
	using Logging;

	public class TaskWorker<T> : IWorkItem<T>
		where T : class, IDisposable
	{
		public virtual int ActiveWorkers
		{
			get { return this._minWorkers; }
		}
		public virtual T State { get; private set; }
		public virtual void PerformOperation(Action operation)
		{
			if (operation == null)
				throw new ArgumentNullException(nameof(operation));

			// FUTURE: watch number of operations and dynamically increase/decrease the number of workers
			if (this._token.IsCancellationRequested)
			{
				Log.Debug("Token cancellation has been requested.");
				this.State.TryDispose();
			}
			else
				operation();
		}

		public TaskWorker(T state, CancellationToken token, int minWorkers, int maxWorkers)
		{
			if (state == null)
				throw new ArgumentNullException(nameof(state));

			this.State = state;
			this._token = token;
			this._minWorkers = minWorkers;
		}

		private static readonly ILog Log = LogFactory.Build(typeof(TaskWorker<>));
		private readonly CancellationToken _token;
		private readonly int _minWorkers;
	}
}