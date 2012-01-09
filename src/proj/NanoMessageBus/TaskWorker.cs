namespace NanoMessageBus
{
	using System;
	using System.Threading;

	public class TaskWorker<T> : IWorkItem<T>
		where T : class, IDisposable
	{
		public virtual int ActiveWorkers
		{
			get { return this.minWorkers; }
		}
		public virtual T State { get; private set; }
		public virtual void PerformOperation(Action operation)
		{
			if (operation == null)
				throw new ArgumentNullException("operation");

			// FUTURE: watch number of operations and dynamically increase/decrease the number of workers
			if (this.token.IsCancellationRequested)
				this.State.Dispose();
			else
				operation();
		}

		public TaskWorker(T state, CancellationToken token, int minWorkers, int maxWorkers)
		{
			if (state == null)
				throw new ArgumentNullException("state");

			this.State = state;
			this.token = token;
			this.minWorkers = minWorkers;
			this.maxWorkers = maxWorkers;
		}

		private readonly CancellationToken token;
		private readonly int minWorkers;
		private readonly int maxWorkers; // FUTURE
	}
}