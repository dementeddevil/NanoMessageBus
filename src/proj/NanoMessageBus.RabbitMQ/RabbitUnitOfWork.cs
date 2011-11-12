namespace NanoMessageBus.RabbitMQ
{
	using System;
	using System.Collections.Generic;
	using Handlers;
	using global::RabbitMQ.Client;
	using global::RabbitMQ.Client.MessagePatterns;

	public partial class RabbitChannel
	{
		private class RabbitUnitOfWork : IHandleUnitOfWork
		{
			public void Register(Action callback)
			{
				this.ThrowWhenDisposed();

				if (callback == null)
					return;

				if (this.transactionType == RabbitTransactionType.None)
					callback();
				else
					this.callbacks.Add(callback);
			}
			public void Complete()
			{
				this.ThrowWhenDisposed();

				foreach (var callback in this.callbacks)
					callback();

				this.Clear();

				this.completed = true;
				this.Acknowledge();
				this.Commit();
			}
			public void Clear()
			{
				this.callbacks.Clear();
			}

			private void Acknowledge()
			{
				if (this.subscription == null)
					return;

				if (this.transactionType == RabbitTransactionType.None)
					return;

				this.subscription.Ack(); // single physical receive allowed per UoW
			}
			private void Commit()
			{
				if (this.transactionType == RabbitTransactionType.Full)
					this.channel.TxCommit();
			}
			private void Rollback()
			{
				if (this.transactionType == RabbitTransactionType.Full)
					this.channel.TxRollback();
			}

			private void ThrowWhenDisposed()
			{
				if (this.disposed)
					throw new ObjectDisposedException(typeof(RabbitUnitOfWork).Name, "The object has already been disposed.");
			}

			public RabbitUnitOfWork(
				IModel channel,
				Subscription subscription,
				RabbitTransactionType transactionType,
				Action cleanup)
			{
				this.channel = channel;
				this.subscription = subscription;
				this.transactionType = transactionType;
				this.cleanup = cleanup ?? (() => { });
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
			private void Dispose(bool disposing)
			{
				if (this.disposed || !disposing)
					return;

				this.disposed = true;
				if (!this.completed)
					this.Rollback();

				this.cleanup();
			}

			private readonly ICollection<Action> callbacks = new LinkedList<Action>();
			private readonly IModel channel;
			private readonly Subscription subscription;
			private readonly RabbitTransactionType transactionType;
			private readonly Action cleanup;
			private bool completed;
			private bool disposed;
		}
	}
}