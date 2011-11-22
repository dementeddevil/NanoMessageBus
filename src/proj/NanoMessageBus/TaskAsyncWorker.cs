namespace NanoMessageBus
{
	using System;
	using System.Collections.Concurrent;
	using System.Threading;

	public class TaskAsyncWorker : IDisposable
	{
		public virtual void Start()
		{
			try
			{
				this.Consume();
			}
			catch (ObjectDisposedException)
			{
			}
			catch (OperationCanceledException)
			{
			}
		}
		protected virtual void Consume()
		{
			foreach (var item in this.queue.GetConsumingEnumerable(this.token))
				item(this.channel);
		}

		public virtual bool AddTask(Action<IMessagingChannel> callback)
		{
			try
			{
				this.queue.Add(callback);
				return true;
			}
			catch (InvalidOperationException)
			{
				return false;
			}
		}
		public virtual void Cancel()
		{
			// TODO: what about all of the worker that has been queued up?
			this.queue.CompleteAdding();
		}

		public TaskAsyncWorker(IMessagingChannel channel, CancellationToken token)
		{
			this.channel = channel;
			this.token = token;
		}
		~TaskAsyncWorker()
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

			try
			{
				this.queue.CompleteAdding();
			}
			catch (ObjectDisposedException)
			{
			}

			this.queue.Dispose();
			this.channel.Dispose();
		}

		private readonly BlockingCollection<Action<IMessagingChannel>> queue =
			new BlockingCollection<Action<IMessagingChannel>>();
		private readonly IMessagingChannel channel;
		private readonly CancellationToken token;
	}
}