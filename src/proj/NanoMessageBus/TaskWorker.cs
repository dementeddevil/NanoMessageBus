using System.Threading.Tasks;

namespace NanoMessageBus
{
	using System;
	using System.Threading;
	using Logging;

	public class TaskWorker<T> : IWorkItem<T>
		where T : class, IDisposable
	{
		public virtual int ActiveWorkers => _minWorkers;

	    public virtual T State { get; private set; }

        public virtual async Task PerformOperation(Func<Task> operation)
		{
			if (operation == null)
			{
			    throw new ArgumentNullException(nameof(operation));
			}

		    // FUTURE: watch number of operations and dynamically increase/decrease the number of workers
			if (_token.IsCancellationRequested)
			{
				Log.Debug("Token cancellation has been requested.");
				State.TryDispose();
			}
			else
			{
			    await operation().ConfigureAwait(false);
			}
		}

		public TaskWorker(T state, CancellationToken token, int minWorkers, int maxWorkers)
		{
			if (state == null)
			{
			    throw new ArgumentNullException(nameof(state));
			}

		    State = state;
			_token = token;
			_minWorkers = minWorkers;
		}

		private static readonly ILog Log = LogFactory.Build(typeof(TaskWorker<>));
		private readonly CancellationToken _token;
		private readonly int _minWorkers;
	}
}