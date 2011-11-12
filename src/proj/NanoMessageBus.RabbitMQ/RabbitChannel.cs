namespace NanoMessageBus.RabbitMQ
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using Handlers;
	using global::RabbitMQ.Client;
	using global::RabbitMQ.Client.Events;
	using global::RabbitMQ.Client.MessagePatterns;

	public partial class RabbitChannel : IDisposable
	{
		public virtual IHandleUnitOfWork UnitOfWork { get; private set; }
		public virtual RabbitMessage CurrentMessage { get; private set; }

		public virtual IHandleUnitOfWork BeginUnitOfWork()
		{
			return this.UnitOfWork ?? (this.UnitOfWork = new RabbitUnitOfWork(
				this.channel, this.subscription, this.options.TransactionType, this.DisposeUnitOfWork));
		}
		private void DisposeUnitOfWork()
		{
			this.UnitOfWork = null;
			this.CurrentMessage = null;
			this.subscription = null;
		}

		public virtual void Send(RabbitMessage message, RabbitAddress receivingAgentExchange)
		{
			if (message == null)
				throw new ArgumentNullException("message");
			if (receivingAgentExchange == null)
				throw new ArgumentNullException("receivingAgentExchange");

			this.ThrowWhenDisposed();

			var properties = this.channel.CreateBasicProperties();

			properties.MessageId = message.MessageId.ToString();
			properties.AppId = message.ProducerId;
			properties.ContentEncoding = message.ContentEncoding;
			properties.ContentType = message.ContentType;
			properties.SetPersistent(message.Durable);

			properties.CorrelationId = message.CorrelationId.ToNull() ?? string.Empty;
			properties.Expiration = message.Expiration.ToUniversalTime().ToString(DateTimeFormat);

			properties.Headers = new Hashtable();
			foreach (var item in message.Headers ?? new Dictionary<string, string>())
				properties.Headers[item.Key] = item.Value;

			if (message.RetryCount > 0)
				properties.Headers[FailureCount] = message.RetryCount;

			properties.ReplyTo = message.ReplyTo;
			properties.Timestamp = new AmqpTimestamp(SystemTime.UtcNow.ToEpochTime());
			properties.Type = message.MessageType;

			var exchange = receivingAgentExchange.Exchange;

			// TODO: try/catch if connection/channel is unavailable
			this.channel.BasicPublish(exchange, message.RoutingKey, properties, message.Body);
		}
		private void ThrowWhenDisposed()
		{
			if (this.disposed)
				throw new ObjectDisposedException(typeof(RabbitChannel).Name, "The object has already been disposed.");
		}

		public virtual RabbitMessage Receive(TimeSpan timeout)
		{
			// TODO: be sure we apply appropriate try/catch semantics here (if channel unavailable/connection lost)
			this.ThrowWhenDisposed();

			BasicDeliverEventArgs result;
			this.OpenSubscription().Next((int)timeout.TotalMilliseconds, out result);
			if (result == null)
				return null;

			var properties = result.BasicProperties;
			var headers = ParseHeaders(properties.Headers);

			return this.CurrentMessage = new RabbitMessage
			{
				MessageId = properties.MessageId.ToGuid(),
				ProducerId = properties.AppId,
				Created = properties.Timestamp.UnixTime.ToDateTime(),
				MessageType = properties.Type,

				ContentEncoding = properties.ContentEncoding,
				ContentType = properties.ContentType,

				Durable = properties.DeliveryMode == 2,
				CorrelationId = properties.CorrelationId.ToGuid(),
				Expiration = properties.Expiration.ToDateTime(),

				Headers = headers,

				ReplyTo = properties.ReplyTo, // TODO: convert to Uri?
				UserId = properties.UserId,

				DeliveryTag = result.DeliveryTag,
				RetryCount = RetryCount(headers, result.Redelivered),
				SourceExchange = result.Exchange,
				RoutingKey = result.RoutingKey,
				Body = result.Body
			};
		}
		private Subscription OpenSubscription()
		{
			var noAck = this.options.TransactionType == RabbitTransactionType.None;
			return this.subscription ?? (this.subscription = new Subscription(
				this.channel, this.options.QueueName, noAck));
		}
		private static IDictionary<string, string> ParseHeaders(IDictionary value)
		{
			if (value == null)
				return null;

			return value.Cast<DictionaryEntry>()
				.Select(x => (string)x.Key)
				.ToDictionary(x => x, y => Encoding.UTF8.GetString((byte[])value[y]));
		}
		private static int RetryCount(IDictionary<string, string> headers, bool redelivered)
		{
			string value;
			if (headers != null && headers.TryGetValue(FailureCount, out value))
				return value.ToInt();

			return redelivered ? 1 : 0;
		}

		public RabbitChannel(Func<object> connectionResolver, RabbitConnectorOptions options)
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

		private const string FailureCount = "x-failure-count";
		private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
		private readonly Func<IConnection> connectionResolver;
		private readonly RabbitConnectorOptions options;
		private IModel channel;
		private Subscription subscription;
		private bool disposed;
	}
}