namespace NanoMessageBus.RabbitChannel
{
	using System;
	using RabbitMQ.Client;

	public class RabbitTransaction : IChannelTransaction
	{
		public virtual bool Finished { get; private set; }

		public virtual void Register(Action callback)
		{
		}
		public virtual void Commit()
		{
		}
		public virtual void Rollback()
		{
		}

		public RabbitTransaction(
			IModel channel, RabbitSubscription subscription, RabbitTransactionType transactionType)
		{
			this.channel = channel;
			this.subscription = subscription;
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
		}

		private readonly IModel channel;
		private readonly RabbitSubscription subscription;
		private readonly RabbitTransactionType transactionType;
	}
}