namespace NanoMessageBus
{
	using System;
	using System.Collections.Concurrent;
	using System.Threading;
	using System.Threading.Tasks;

	public class TaskWorkerGroup : IDisposable
	{
		public virtual void StartWorker(Func<IMessagingChannel> factory)
		{
			if (factory == null)
				throw new ArgumentNullException("factory");

			TaskAsyncWorker worker = null;

			try
			{
				worker = this.BeginWork(factory);

				var task = Task.Factory.StartNew(
					() => worker.Start(),
					this.cancellationSource.Token,
					TaskCreationOptions.LongRunning,
					TaskScheduler.Default);

				this.tasks.Add(task);
			}
			catch (ChannelConnectionException)
			{
				// TODO: how to remove?
				if (worker != null)
					worker.Dispose();

				throw;
			}
		}
		protected virtual TaskAsyncWorker BeginWork(Func<IMessagingChannel> factory)
		{
			var channel = factory();
			var worker = new TaskAsyncWorker(channel, this.cancellationSource.Token);
			this.workers.Add(worker);
			worker.Start();
			return worker;
		}

		public virtual void AddWorkToAny(Action<IMessagingChannel> workItem)
		{
			// TODO
			if (workItem == null)
				throw new ArgumentNullException("workItem");
		}

		public virtual void AddWorkToAll(Action<IMessagingChannel> workItem)
		{
			if (workItem == null)
				throw new ArgumentNullException("workItem");

			foreach (var worker in this.workers)
				worker.AddTask(workItem);
		}

		protected virtual void StopWorkers()
		{
			foreach (var worker in this.workers)
				worker.Dispose();
		}

		public TaskWorkerGroup()
		{
		}
		~TaskWorkerGroup()
		{
		}

		public void Dispose()
		{
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
				return;

			this.cancellationSource.Cancel();
		}

		private readonly ConcurrentBag<Task> tasks = new ConcurrentBag<Task>();
		private readonly ConcurrentBag<TaskAsyncWorker> workers = new ConcurrentBag<TaskAsyncWorker>();
		private readonly CancellationTokenSource cancellationSource = new CancellationTokenSource();
	}
}