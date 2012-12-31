namespace NanoMessageBus.Channels
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using Logging;
	using RabbitMQ.Client;
	using RabbitMQ.Client.Events;
	using RabbitMQ.Client.Exceptions;

	public class RabbitChannel : IMessagingChannel
	{
		public virtual bool Active
		{
			get { return !this.disposed && !this.shutdown && this.connector.CurrentState == ConnectionState.Open; }
		}
		public virtual ChannelMessage CurrentMessage { get; private set; }
		public virtual IDependencyResolver CurrentResolver { get; private set; }
		public virtual IChannelTransaction CurrentTransaction { get; private set; }
		public virtual IChannelGroupConfiguration CurrentConfiguration { get; private set; }

		public virtual void Receive(Action<IDeliveryContext> callback)
		{
			if (callback == null)
				throw new ArgumentNullException("callback");

			Log.Debug("Attempting to begin receiving messages on channel {0}.", this.identifier);

			this.ThrowWhenDisposed();
			this.ThrowWhenDispatchOnly();
			this.ThrowWhenSubscriptionExists();
			this.ThrowWhenShuttingDown();

			this.Try(() =>
			{
				this.subscription = this.subscriptionFactory();
				this.subscription.Receive(this.configuration.ReceiveTimeout, msg =>
					this.Receive(msg, callback));
			});
		}
		protected virtual bool Receive(BasicDeliverEventArgs message, Action<IDeliveryContext> callback)
		{
			this.CurrentMessage = null;
			this.delivery = message;

			if (this.shutdown)
			{
				Log.Debug("Shutdown request has been made on channel {0}; finished receiving.", this.identifier);
				return FinishedReceiving;
			}

			if (message == null)
			{
				this.ThrowWhenClosed();
				return ContinueReceiving;
			}

			this.EnsureTransaction();
			this.TryReceive(message, callback);

			return !this.shutdown;
		}
		protected virtual void TryReceive(BasicDeliverEventArgs message, Action<IDeliveryContext> callback)
		{
			var messageId = message.MessageId();

			try
			{
				Log.Verbose("Translating wire-specific message into channel message for channel {0}.", this.identifier);
				this.CurrentMessage = this.adapter.Build(message);

				Log.Info("Routing message '{0}' received through group '{1}' to configured receiver callback on channel {2}.",
					messageId, this.configuration.GroupName, this.identifier);
				callback(this);
			}
			catch (ChannelConnectionException)
			{
				Log.Warn("Channel {0} has become unavailable, aborting current transaction.", this.identifier);
				this.CurrentTransaction.TryDispose();
				throw;
			}
			catch (PoisonMessageException e)
			{
				Log.Warn("Wire message {0} on channel {1} could not be deserialized; forwarding to poison message exchange.", messageId, this.identifier);
				this.ForwardToPoisonMessageExchange(message, e);
			}
			catch (DeadLetterException e)
			{
				var seconds = (SystemTime.UtcNow - e.Expiration).TotalSeconds;
				Log.Info("Wire message {0} on channel {1} expired on the wire {2:n3} seconds ago; forwarding to dead letter exchange.", messageId, this.identifier, seconds);
				this.ForwardTo(message, this.configuration.DeadLetterExchange);
			}
			catch (Exception e)
			{
				this.RetryMessage(message, e);
			}
		}
		protected virtual void RetryMessage(BasicDeliverEventArgs message, Exception exception)
		{
			var nextAttempt = this.AppendException(message, exception) + 1;
			Log.Debug("Message '{0}' has been attempted {1} times on channel {2}.", message.MessageId(), nextAttempt, this.identifier);

			if (nextAttempt > this.configuration.MaxAttempts)
			{
				Log.Error("Unable to process message '{0}' on channel {1}".FormatWith(message.MessageId(), this.identifier), exception);
				this.ForwardToPoisonMessageExchange(message, null);
			}
			else
			{
				Log.Info("Unhandled exception for message '{0}' on channel {1}; retrying.".FormatWith(message.MessageId(), this.identifier), exception);
				this.ForwardTo(message, this.configuration.InputQueue.ToPublicationAddress());
			}
		}
		protected virtual void ForwardToPoisonMessageExchange(BasicDeliverEventArgs message, Exception exception)
		{
			Log.Info("Message '{0}' on channel {1} is a poison message.", message.MessageId(), this.identifier);

			this.AppendException(message, exception);
			message.SetAttemptCount(0);
			this.adapter.AppendRetryAddress(message);

			this.ForwardTo(message, this.configuration.PoisonMessageExchange);
		}
		protected virtual int AppendException(BasicDeliverEventArgs message, Exception exception)
		{
			if (exception == null)
				return 0;

			var currentAttempt = message.GetAttemptCount();
			message.SetAttemptCount(currentAttempt + 1); // 1-based value

			this.adapter.AppendException(message, exception, currentAttempt);

			return currentAttempt;
		}

		protected virtual void ForwardTo(BasicDeliverEventArgs message, PublicationAddress address)
		{
			Log.Debug("Forwarding message '{0}' on channel {1} to recipient '{2}'.", message.MessageId(), this.identifier, address);

			this.EnsureTransaction();
			this.Send(message, address);
			this.CurrentTransaction.Commit();
		}

		public virtual IDispatchContext PrepareDispatch(object message = null, IMessagingChannel actual = null)
		{
			this.EnsureTransaction();

			var context = new DefaultDispatchContext(actual ?? this);
			return message == null ? context : context.WithMessage(message);
		}

		public virtual void Send(ChannelEnvelope envelope)
		{
			if (envelope == null)
				throw new ArgumentNullException("envelope");

			this.ThrowWhenDisposed();

			if (this.subscription == null)
				this.ThrowWhenShuttingDown();

			this.EnsureTransaction().Register(() => this.EnlistSend(envelope));
		}
		private void EnlistSend(ChannelEnvelope envelope)
		{
			var message = this.CurrentMessage == envelope.Message
				? this.delivery
				: this.adapter.Build(envelope.Message, this.channel.CreateBasicProperties());

			Log.Verbose("Sending wire message '{0}' on channel {1} to {2} recipients.",
				message.MessageId(), this.identifier, envelope.Recipients.Count);
			foreach (var recipient in envelope.Recipients.Select(x => x.ToPublicationAddress(this.configuration)))
			{
				this.ThrowWhenDisposed();
				this.Send(message, recipient);
			}
		}
		protected virtual void Send(BasicDeliverEventArgs message, PublicationAddress recipient)
		{
			if (recipient == null)
				return;

			if (recipient == this.configuration.DeadLetterExchange)
				this.adapter.AppendRetryAddress(message);
			else if (recipient == this.configuration.UnhandledMessageExchange)
				this.adapter.AppendRetryAddress(message);

			this.EnsureTransaction().Register(() => this.Try(() =>
			{
				Log.Info("Dispatching wire message '{0}' on channel {1} to messaging infrastructure for recipient '{2}'.",
					message.MessageId(), this.identifier, recipient);
				this.channel.BasicPublish(recipient, message.BasicProperties, message.Body);
			}));
		}

		public virtual void AcknowledgeMessage()
		{
			Log.Verbose("Requesting acknowledgement of received messages against messaging infrastructure on channel {0}.", this.identifier);
			this.ThrowWhenDisposed();

			if (this.subscription == null || this.transactionType == RabbitTransactionType.None)
				return;

			Log.Verbose("Acknowledging all previous message deliveries from the messaging infrastructure on channel {0}.", this.identifier);
			this.Try(this.subscription.AcknowledgeMessages);
		}
		public virtual void CommitTransaction()
		{
			Log.Verbose("Requesting commit of transaction against messaging infrastructure on channel {0}.", this.identifier);
			this.ThrowWhenDisposed();

			if (this.transactionType == RabbitTransactionType.Full)
			{
				Log.Verbose("Committing transaction against the messaging infrastructure on channel {0}.", this.identifier);
				this.Try(this.channel.TxCommit);
			}

			this.EnsureTransaction();
		}
		public virtual void RollbackTransaction()
		{
			Log.Verbose("Requesting rollback of transaction against messaging infrastructure on channel {0}.", this.identifier);
			this.Try(() =>
			{
				this.ThrowWhenDisposed();

				if (this.transactionType == RabbitTransactionType.Full)
				{
					Log.Verbose("Rolling back transaction against the messaging infrastructure on channel {0}.", this.identifier);
					this.channel.TxRollback();
				}

				this.EnsureTransaction();
			});
		}

		public virtual void BeginShutdown()
		{
			Log.Debug("Beginning shutdown sequence on channel {0}.", this.identifier);
			this.shutdown = true;
		}

		protected virtual void ThrowWhenClosed()
		{
			var reason = this.channel.CloseReason;
			if (reason == null)
				return;

			throw new OperationInterruptedException(reason);
		}
		protected virtual void ThrowWhenDispatchOnly()
		{
			if (!this.configuration.DispatchOnly)
				return;

			Log.Warn("Channel {0} is dispatch only and cannot receive messages.", this.identifier);
			throw new InvalidOperationException("Dispatch-only channels cannot receive messages.");
		}
		protected virtual void ThrowWhenShuttingDown()
		{
			if (!this.shutdown)
				return;

			Log.Warn("Channel {0} is shutting down.", this.identifier);
			throw new ChannelShutdownException();
		}
		protected virtual void ThrowWhenDisposed()
		{
			if (!this.disposed)
				return;

			Log.Warn("Channel {0} has previously been disposed.", this.identifier);
			throw new ObjectDisposedException(typeof(RabbitChannel).Name);
		}
		protected virtual void ThrowWhenSubscriptionExists()
		{
			if (this.subscription == null)
				return;

			Log.Warn("A receive callback has already been specified on channel {0}.", this.identifier);
			throw new InvalidOperationException("The channel already has a receive callback.");
		}

		protected virtual IChannelTransaction EnsureTransaction()
		{
			if (!this.CurrentTransaction.Finished)
				return this.CurrentTransaction;

			Log.Verbose("The current transaction has been completed, creating a new transaction on channel {0}.", this.identifier);

			this.CurrentTransaction.TryDispose();
			return this.CurrentTransaction = new RabbitTransaction(this, this.transactionType);
		}
		protected virtual void Try(Action callback)
		{
			try
			{
				if (this.suppressOperations)
					Log.Debug("Channel {0} is unstable, suppressing further communication.", this.identifier);
				else
					callback();
			}
			catch (IOException e)
			{
				this.ShutdownChannel("Channel operation failed, aborting channel {0}. Further operations will be suppressed.", e);
			}
			catch (OperationInterruptedException e)
			{
				this.ShutdownChannel("Channel operation interrupted, aborting channel {0}. Further operations will be suppressed.", e);
			}
		}
		private void ShutdownChannel(string message, Exception e)
		{
			Log.Info(message, this.identifier);
			this.suppressOperations = true;
			this.Dispose();
			throw new ChannelConnectionException(e.Message, e);
		}

		public RabbitChannel(
			IModel channel,
			IChannelConnector connector,
			RabbitChannelGroupConfiguration configuration,
			Func<RabbitSubscription> subscriptionFactory) : this()
		{
			this.channel = channel;
			this.connector = connector;
			this.CurrentConfiguration = this.configuration = configuration;
			this.adapter = configuration.MessageAdapter;
			this.transactionType = configuration.TransactionType;
			this.subscriptionFactory = subscriptionFactory;
			this.CurrentResolver = configuration.DependencyResolver;
			this.identifier = Interlocked.Increment(ref counter);

			this.CurrentTransaction = new RabbitTransaction(this, this.transactionType);
			if (this.transactionType == RabbitTransactionType.Full)
			{
				Log.Debug("Marking channel {0} as transactional.", this.identifier);
				this.channel.TxSelect();
			}

			if (this.configuration.ChannelBuffer <= 0 || this.configuration.DispatchOnly)
				return;

			var buffer = this.transactionType == RabbitTransactionType.None ? long.MaxValue : this.configuration.ChannelBuffer;
			Log.Debug("Buffering up to {0} message(s) on the channel {1}.", buffer, this.identifier);
			if (this.configuration.TransactionType == RabbitTransactionType.None)
				return;

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

			Log.Debug("Disposing channel {0}.", this.identifier);
			this.CurrentTransaction.TryDispose(); // must happen here because it checks for dispose

			this.disposed = true;

			if (this.subscription != null)
				this.subscription.TryDispose();

			// dispose can throw while abort does the exact same thing without throwing
			this.channel.Abort();

			Log.Debug("Channel {0} disposed.", this.identifier);
		}

		private const bool ContinueReceiving = true;
		private const bool FinishedReceiving = false; // returning false means the receiving handler will exit.
		private static readonly ILog Log = LogFactory.Build(typeof(RabbitChannel));
		private static int counter;
		private readonly IModel channel;
		private readonly IChannelConnector connector;
		private readonly RabbitMessageAdapter adapter;
		private readonly RabbitChannelGroupConfiguration configuration;
		private readonly RabbitTransactionType transactionType;
		private readonly Func<RabbitSubscription> subscriptionFactory;
		private readonly int identifier;
		private RabbitSubscription subscription;
		private BasicDeliverEventArgs delivery;
		private bool suppressOperations;
		private bool disposed;
		private volatile bool shutdown;
	}
}