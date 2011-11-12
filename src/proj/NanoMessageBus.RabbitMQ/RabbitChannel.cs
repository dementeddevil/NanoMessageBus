namespace NanoMessageBus.RabbitMQ
{
	using System;
	using Handlers;
	using global::RabbitMQ.Client;
	using global::RabbitMQ.Client.Events;
	using global::RabbitMQ.Client.MessagePatterns;

	public partial class RabbitChannel : IDisposable
	{
		public virtual RabbitMessage CurrentMessage { get; private set; }

		public virtual IHandleUnitOfWork UnitOfWork
		{
			get { return this.BeginUnitOfWork(); }
		}
		protected virtual IHandleUnitOfWork BeginUnitOfWork()
		{
			return this.unitOfWork ?? (this.unitOfWork = new RabbitUnitOfWork(
				this.channel, () => this.subscription, this.options.TransactionType, this.DisposeUnitOfWork));
		}
		protected virtual void DisposeUnitOfWork()
		{
			this.unitOfWork = null;
			this.CurrentMessage = null;
			this.subscription = null;
		}

		public virtual void Send(RabbitMessage message, string receivingAgentExchange)
		{
			if (message == null)
				throw new ArgumentNullException("message");
			if (string.IsNullOrEmpty(receivingAgentExchange))
				throw new ArgumentNullException("receivingAgentExchange");
			this.ThrowWhenDisposed();

			// TODO: try/catch if connection/channel is unavailable
			var serialized = message.SerializeProperties(this.channel);
			var routingKey = message.RoutingKey ?? string.Empty;
			this.channel.BasicPublish(receivingAgentExchange, routingKey, serialized, message.Body);
		}
		protected virtual void ThrowWhenDisposed()
		{
			if (this.disposed)
				throw new ObjectDisposedException(typeof(RabbitChannel).Name, "The object has already been disposed.");
		}

		public virtual RabbitMessage Receive(TimeSpan timeout)
		{
			this.ThrowWhenDisposed();

			// TODO: be sure we apply appropriate try/catch semantics here (if channel unavailable/connection lost)
			BasicDeliverEventArgs delivery;
			this.OpenSubscription().Next((int)timeout.TotalMilliseconds, out delivery);
			return this.CurrentMessage = (delivery == null ? null : new RabbitMessage(delivery));
		}
		private Subscription OpenSubscription()
		{
			// TODO: be sure we apply appropriate try/catch semantics here (if channel unavailable/connection lost)
			var noAck = this.options.TransactionType == RabbitTransactionType.None;
			return this.subscription ?? (this.subscription = new Subscription(
				this.channel, this.options.QueueName, noAck));
		}

		public RabbitChannel(Func<object> connectionResolver, RabbitWireup options)
		{
			this.connectionResolver = () => connectionResolver() as IConnection;
			this.options = options;

			this.EstablishChannel();
		}
		~RabbitChannel()
		{
			this.Dispose(false);
		}
		private void EstablishChannel()
		{
			// TODO: figure out a thread-safe way for the RabbitConnector to signal this instance
			// instructing it to dispose of itself and shutdown and remove itself from thread storage.
			// TODO: check connection state to ensure we can open?

			// TODO: if this yields null, wait for a signal before we can re-establish the connection
			var connection = this.connectionResolver();

			this.channel = connection.CreateModel();
			this.channel.BasicQos(0, (ushort)this.options.PrefetchCount, false);

			if (this.options.TransactionType == RabbitTransactionType.Full)
				this.channel.TxSelect();
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
			this.channel.Dispose();

			var storage = new ThreadStorage();
			storage.Remove(RabbitConnector.ThreadKey);
		}
		
		private readonly Func<IConnection> connectionResolver;
		private readonly RabbitWireup options;
		
		private IModel channel;
		private Subscription subscription;
		private IHandleUnitOfWork unitOfWork;

		private bool disposed;
	}
}