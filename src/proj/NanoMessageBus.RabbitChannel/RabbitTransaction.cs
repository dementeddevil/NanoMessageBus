namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections.Generic;
	using Logging;

	public class RabbitTransaction : IChannelTransaction
	{
		public virtual bool Finished
		{
			get { return this.committed || this.rolledBack || this.disposed; }
		}

		public virtual void Register(Action callback)
		{
			if (callback == null)
				throw new ArgumentNullException("callback");

			this.ThrowWhenDisposed();
			this.ThrowWhenRolledBack();
			this.ThrowWhenCommitted();

			if (this.transactionType == RabbitTransactionType.None)
				callback();
			else
				this.callbacks.Add(callback);
		}

		public virtual void Commit()
		{
			this.ThrowWhenDisposed();
			this.ThrowWhenRolledBack();

			this.committed = true;

			try
			{
				this.TryCommit();
			}
			finally
			{
				this.callbacks.Clear();
			}
		}
		protected virtual void TryCommit()
		{
			foreach (var callback in this.callbacks)
				callback();

			this.AcknowledgeReceipt();
			this.CommitChannel();
		}
		protected virtual void AcknowledgeReceipt()
		{
			if (this.transactionType == RabbitTransactionType.None)
				return;

			Log.Verbose("Attempting to acknowledge receipt of all received messages against the underlying channel.");
			this.channel.AcknowledgeMessage();
		}
		protected virtual void CommitChannel()
		{
			if (this.transactionType != RabbitTransactionType.Full)
				return;

			Log.Verbose("Attempting to commit the transaction against the underlying channel.");
			this.channel.CommitTransaction();
		}

		public virtual void Rollback()
		{
			if (this.rolledBack)
				return;

			this.ThrowWhenCommitted();
			this.ThrowWhenDisposed();

			this.rolledBack = true; 

			try
			{
				this.RollbackChannel();
			}
			finally
			{
				this.callbacks.Clear();
			}
		}
		protected virtual void RollbackChannel()
		{
			if (this.transactionType == RabbitTransactionType.Full)
				this.channel.RollbackTransaction();
		}

		protected virtual void ThrowWhenDisposed()
		{
			if (!this.disposed)
				return;

			Log.Warn("The transaction has already been disposed.");
			throw new ObjectDisposedException(typeof(RabbitTransaction).Name);
		}
		protected virtual void ThrowWhenRolledBack()
		{
			if (!this.rolledBack)
				return;

			Log.Warn("Cannot perform this operation on a rolled-back transaction.");
			throw new InvalidOperationException("Cannot perform this operation on a rolled-back transaction.");
		}
		protected virtual void ThrowWhenCommitted()
		{
			if (!this.committed)
				return;

			Log.Warn("The transaction has already committed.");
			throw new InvalidOperationException("The transaction has already committed.");
		}

		public RabbitTransaction(RabbitChannel channel, RabbitTransactionType transactionType)
		{
			this.channel = channel;
			this.transactionType = transactionType;
		}
		~RabbitTransaction()
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
			if (!disposing || this.disposed)
				return;

			this.disposed = true;
			this.callbacks.Clear();

			if (this.committed || this.rolledBack)
				return;

			try
			{
				this.RollbackChannel();
			}
			catch (ChannelConnectionException)
			{
			}
			catch (ObjectDisposedException)
			{
			}
		}

		private static readonly ILog Log = LogFactory.Build(typeof(RabbitTransaction));
		private readonly ICollection<Action> callbacks = new LinkedList<Action>();
		private readonly RabbitChannel channel;
		private readonly RabbitTransactionType transactionType;
		private bool disposed;
		private bool committed;
		private bool rolledBack;
	}
}