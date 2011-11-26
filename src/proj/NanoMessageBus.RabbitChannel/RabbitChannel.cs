namespace NanoMessageBus.RabbitChannel
{
	using System;
	using System.Linq;
	using System.Runtime.Serialization;
	using RabbitMQ.Client;
	using RabbitMQ.Client.Events;

	public class RabbitChannel : IMessagingChannel
	{
		public virtual ChannelMessage CurrentMessage { get; private set; }
		public virtual IChannelTransaction CurrentTransaction { get; private set; }

		public virtual void Receive(Action<IDeliveryContext> callback)
		{
			if (callback == null)
				throw new ArgumentNullException("callback");

			this.ThrowWhenDisposed();
			this.ThrowWhenSubscriptionExists();
			this.ThrowWhenShuttingDown();

			this.Try(() =>
			{
				this.subscription = this.subscriptionFactory();
				this.subscription.BeginReceive(this.configuration.ReceiveTimeout, msg =>
					this.Receive(msg, callback));
			});
		}
		protected virtual bool Receive(BasicDeliverEventArgs message, Action<IDeliveryContext> callback)
		{
			this.CurrentMessage = null;

			if (this.shutdown)
				return false;

			if (message == null)
				return true;

			using (this.NewTransaction())
				this.TryReceive(message, callback);

			return this.shutdown == false;
		}
		protected virtual void TryReceive(BasicDeliverEventArgs message, Action<IDeliveryContext> callback)
		{
			try
			{
				this.CurrentMessage = this.adapter.Build(message);
				callback(this);
				this.adapter.PurgeFromCache(message);
			}
			catch (ChannelConnectionException)
			{
				this.adapter.PurgeFromCache(message);
				throw;
			}
			catch (SerializationException e)
			{
				this.ForwardToPoisonMessageExchange(message, e);
			}
			catch (Exception e)
			{
				this.RetryMessage(message, e);
			}
		}
		protected virtual void RetryMessage(BasicDeliverEventArgs message, Exception exception)
		{
			var nextAttempt = message.GetAttemptCount() + 1;
			message.SetAttemptCount(nextAttempt);

			if (nextAttempt > this.configuration.MaxAttempts)
				this.ForwardToPoisonMessageExchange(message, exception);
			else
				this.subscription.RetryMessage(message);
		}
		protected virtual void ForwardToPoisonMessageExchange(BasicDeliverEventArgs message, Exception exception)
		{
			message.SetAttemptCount(0);
			this.adapter.AppendException(message, exception);

			this.NewTransaction();
			this.Send(message, this.configuration.PoisonMessageExchange);
			this.CurrentTransaction.Commit();

			this.adapter.PurgeFromCache(message);
		}

		public virtual void Send(ChannelEnvelope envelope)
		{
			if (envelope == null)
				throw new ArgumentNullException("envelope");

			this.ThrowWhenDisposed();

			if (this.subscription == null)
				this.ThrowWhenShuttingDown();

			var message = this.adapter.Build(envelope.Message, this.channel.CreateBasicProperties());

			foreach (var recipient in envelope.Recipients.Select(x => new RabbitAddress(x)))
			{
				this.ThrowWhenDisposed();
				this.Send(message, recipient);
			}
		}
		protected virtual void Send(BasicDeliverEventArgs message, RabbitAddress recipient)
		{
			if (this.CurrentTransaction.Finished)
				this.NewTransaction();

			this.CurrentTransaction.Register(() => this.Try(() =>
			    this.channel.BasicPublish(recipient.Address, message.BasicProperties, message.Body)));
		}

		public virtual void AcknowledgeMessage()
		{
			this.ThrowWhenDisposed();
			this.ThrowWhenSubscriptionMissing();

			if (this.transactionType != RabbitTransactionType.None)
				this.Try(this.subscription.AcknowledgeMessage);
		}
		public virtual void CommitTransaction()
		{
			this.ThrowWhenDisposed();

			if (this.transactionType == RabbitTransactionType.Full)
				this.Try(this.channel.TxCommit);

			this.NewTransaction();
		}
		public virtual void RollbackTransaction()
		{
			this.ThrowWhenDisposed();

			if (this.transactionType == RabbitTransactionType.Full)
				this.Try(this.channel.TxRollback);

			this.NewTransaction();
		}

		public virtual void BeginShutdown()
		{
			this.shutdown = true;
		}

		protected virtual void ThrowWhenShuttingDown()
		{
			if (this.shutdown)
				throw new ChannelShutdownException();
		}
		protected virtual void ThrowWhenDisposed()
		{
			if (this.disposed)
				throw new ObjectDisposedException("RabbitChannel");
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

		protected virtual IChannelTransaction NewTransaction()
		{
			return this.CurrentTransaction = new RabbitTransaction(this, this.transactionType);
		}
		protected virtual void Try(Action callback)
		{
			callback(); // TODO: catch and wrap RabbitMQ.Client-specific exceptions in a ChannelUnavailableException
		}

		public RabbitChannel(
			IModel channel,
			RabbitMessageAdapter adapter,
			RabbitChannelGroupConfiguration configuration,
			Func<RabbitSubscription> subscriptionFactory) : this()
		{
			this.channel = channel;
			this.adapter = adapter;
			this.configuration = configuration;
			this.transactionType = configuration.TransactionType;
			this.subscriptionFactory = subscriptionFactory;

			this.CurrentTransaction = new RabbitTransaction(this, this.transactionType);

			if (this.transactionType == RabbitTransactionType.Full)
				this.channel.TxSelect();

			if (this.configuration.ChannelBuffer > 0)
				this.channel.BasicQos(0, (ushort)this.configuration.ChannelBuffer, false);
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

			if (this.disposed)
				return;

			this.disposed = true;

			if (this.subscription != null)
				this.subscription.Dispose();

			this.channel.Dispose();
		}

		private readonly IModel channel;
		private readonly RabbitMessageAdapter adapter;
		private readonly RabbitChannelGroupConfiguration configuration;
		private readonly RabbitTransactionType transactionType;
		private readonly Func<RabbitSubscription> subscriptionFactory;
		private RabbitSubscription subscription;
		private bool disposed;
		private volatile bool shutdown;
	}
}