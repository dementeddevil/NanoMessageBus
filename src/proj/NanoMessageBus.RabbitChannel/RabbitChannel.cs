namespace NanoMessageBus.RabbitChannel
{
	using System;
	using RabbitMQ.Client;
	using RabbitMQ.Client.Events;

	public class RabbitChannel : IMessagingChannel
	{
		public virtual ChannelMessage CurrentMessage { get; private set; }
		public virtual IChannelTransaction CurrentTransaction { get; private set; }

		public virtual void BeginReceive(Action<IDeliveryContext> callback)
		{
			if (callback == null)
				throw new ArgumentNullException("callback");

			this.ThrowWhenSubscriptionExists();

			// TODO: wrap up the exceptions on the following calls if the channel is unavailable
			this.subscription = this.subscriptionFactory();
			this.subscription.BeginReceive<BasicDeliverEventArgs>(DefaultTimeout, msg =>
				this.BeginReceive(msg, callback));
		}
		protected virtual void BeginReceive<T>(T message, Action<IDeliveryContext> callback) where T : class
		{
			this.CurrentTransaction = new RabbitTransaction(this, this.transactionType);
			this.CurrentMessage = null; // TODO: convert from BasicDeliverEventArgs

			// TODO: on serialization failure, immediately forward to poison message exchange
			// and ack/commit
			try
			{
				// TODO: *after* callback:
				// 1. clear failure count for message (global per app or at least shared per channel group)
				// 2. clear serialization cache for message (global per app or at least shared per channel group)
				callback(this);
			}
			catch (ChannelConnectionException)
			{
				throw;
			}
			catch
			{
				// TODO: increment failure count; if it exceeds configured amount, forward to poison message exchange (along with serialization info)
				// adding message back to in-memory queue means another channel (within the same channel group)
				// can pick it up for processing, therefore failure/serialization caches must be shared
				this.subscription.RetryMessage(message); // TODO: if channel is unavailable
			}
		}

		public virtual void Send(ChannelEnvelope envelope)
		{
			// TODO: convert then channel.BasicPublish() to each destination
		}

		public virtual void AcknowledgeMessage()
		{
			this.ThrowWhenSubscriptionMissing();
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

		protected virtual void ThrowWhenSubscriptionExists()
		{
			if (this.subscription != null)
				throw new InvalidOperationException("The channel already has a receive callback.");
		}
		protected virtual void ThrowWhenSubscriptionMissing()
		{
			if (this.subscription == null)
				throw new InvalidOperationException("The channel must first be opened for receive.");
		}

		public RabbitChannel(
			IModel channel,
			RabbitChannelGroupConfiguration configuration,
			Func<RabbitSubscription> subscriptionFactory) : this()
		{
			this.channel = channel;
			this.configuration = configuration;
			this.transactionType = configuration.TransactionType;
			this.subscriptionFactory = subscriptionFactory;

			if (this.transactionType == RabbitTransactionType.Full)
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
		private readonly RabbitChannelGroupConfiguration configuration;
		private readonly RabbitTransactionType transactionType;
		private readonly Func<RabbitSubscription> subscriptionFactory;
		private RabbitSubscription subscription;
		private bool disposed;
	}
}