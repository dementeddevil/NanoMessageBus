namespace NanoMessageBus.RabbitChannel
{
	using System;
	using RabbitMQ.Client;

	public class RabbitChannel : IMessagingChannel
	{
		public virtual ChannelMessage CurrentMessage { get; private set; }
		public virtual IChannelTransaction CurrentTransaction { get; private set; }

		public virtual void BeginReceive(Action<IDeliveryContext> callback)
		{
			if (callback == null)
				throw new ArgumentNullException("callback");

			if (this.subscription != null)
				throw new InvalidOperationException("The channel already has a receive callback.");

			this.subscription = this.subscriptionFactory(); // TODO: wrap exception if channel unavailable
			this.subscription.BeginReceive(DefaultTimeout, msg =>
			{
				this.CurrentMessage = null; // TODO: convert from RabbitMessage
				this.CurrentTransaction = null; // TODO: start new transaction

				callback(this);
			});
		}
		public virtual void Send(ChannelEnvelope envelope)
		{
		}

		public virtual void AcknowledgeMessage()
		{
			if (this.subscription == null)
				throw new InvalidOperationException("The channel must first be opened for receive.");

			if (this.transactionType != RabbitTransactionType.None)
				this.subscription.AcknowledgeMessage(); // TODO: wrap exception if channel unavailable
		}
		public virtual void CommitTransaction()
		{
			if (this.transactionType == RabbitTransactionType.Full)
				this.channel.TxCommit(); // TODO: wrap exception if channel unavailable
		}
		public virtual void RollbackTransaction()
		{
			if (this.transactionType == RabbitTransactionType.Full)
			    this.channel.TxRollback(); // TODO: wrap exception if channel unavailable
		}

		public RabbitChannel(
			IModel channel,
			RabbitTransactionType transactionType,
			Func<RabbitSubscription> subscriptionFactory) : this()
		{
			this.channel = channel;
			this.transactionType = transactionType;
			this.subscriptionFactory = subscriptionFactory;

			if (transactionType == RabbitTransactionType.Full)
				this.channel.TxSelect();
		}
		protected RabbitChannel() { }
		~RabbitChannel()
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

				if (this.subscription != null)
					this.subscription.Dispose();

				this.channel.Dispose();
			}
		}

		private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(500);
		private readonly object locker = new object();
		private readonly IModel channel;
		private readonly RabbitTransactionType transactionType;
		private readonly Func<RabbitSubscription> subscriptionFactory;
		private RabbitSubscription subscription;
		private bool disposed;
	}
}