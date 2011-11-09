namespace NanoMessageBus.RabbitMQ
{
	using System;
	using System.Collections.Generic;
	using Handlers;
	using global::RabbitMQ.Client;

	public class RabbitUnitOfWork : IHandleUnitOfWork
	{
		public virtual void Register(Action callback)
		{
			if (callback != null)
				this.callbacks.Add(callback);
		}
		public virtual void Complete()
		{
			foreach (var callback in this.callbacks)
				callback();

			this.Clear();
			this.completed = true;
			this.channel.TxCommit();
		}
		public virtual void Clear()
		{
			this.callbacks.Clear();
		}

		public RabbitUnitOfWork(IModel channel)
		{
			this.channel = channel;
		}
		~RabbitUnitOfWork()
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
			if (this.disposed || !disposing)
				return;

			this.disposed = true;
			if (!this.completed)
				this.channel.TxRollback();
		}

		private readonly ICollection<Action> callbacks = new LinkedList<Action>();
		private readonly IModel channel;
		private bool completed;
		private bool disposed;
	}
}