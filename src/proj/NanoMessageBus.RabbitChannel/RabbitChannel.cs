namespace NanoMessageBus.RabbitChannel
{
	using System;
	using System.Linq;
	using System.Runtime.Serialization;
	using System.Threading;
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

			if (this.shutdown == ShuttingDown)
				return false;

			if (message == null)
				return true;

			using (this.NewTransaction())
				this.TryReceive(message, callback);

			return this.shutdown == KeepAlive;
		}
		protected virtual void TryReceive(BasicDeliverEventArgs message, Action<IDeliveryContext> callback)
		{
			try
			{
				this.CurrentMessage = this.adapter.Build(message);

				// TODO: *after* callback:
				// 1. clear failure count for message (global per app or at least shared per channel group)
				// 2. clear serialization cache for message (global per app or at least shared per channel group)
				callback(this);
			}
			catch (SerializationException)
			{
				this.Send(message, this.configuration.PoisonMessageExchange); // TODO: add exception headers
				this.CurrentTransaction.Commit();
			}
			catch (ChannelConnectionException)
			{
				throw; // this isn't something that can ben retried; TODO: should the channel be disposed here?
			}
			catch
			{
				// TODO: add exception headers

				// TODO: increment failure count; if it exceeds configured amount, forward to poison message exchange (along with serialization info)
				// adding message back to in-memory queue means another channel (within the same channel group)
				// can pick it up for processing, therefore failure/serialization caches must be shared
				this.subscription.RetryMessage(message);
			}
		}

		public virtual void Send(ChannelEnvelope envelope)
		{
			if (envelope == null)
				throw new ArgumentNullException("envelope");

			this.ThrowWhenDisposed();

			if (this.subscription == null)
				this.ThrowWhenShuttingDown();

			var message = this.adapter.Build(envelope.Message);

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
			// shutdown uses the pattern "volatile int32 reads, with cmpxch writes"
			// which is safe for updates and cannot suffer torn reads.
			// see CancellationTokenSource.cs in BCL Task Parallel Library source code.
			Interlocked.Exchange(ref this.shutdown, ShuttingDown);
		}

		protected virtual void ThrowWhenShuttingDown()
		{
			if (this.shutdown == ShuttingDown)
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
			// TODO: catch the appropriate exception(s) and wrap them up as a ChannelUnavailableException
			callback();
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
		private volatile int shutdown;
		private const int ShuttingDown = 1;
		private const int KeepAlive = 0;
	}
}