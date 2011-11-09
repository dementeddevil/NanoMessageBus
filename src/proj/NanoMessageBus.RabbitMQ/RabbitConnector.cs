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
	
	public partial class RabbitConnector : IDisposable
	{
		//// TODO: logging
		//// TODO: we need to be sure we apply appropriate try/catch semantics here (if channel unavailable/connection lost)

		public IHandleUnitOfWork UnitOfWork { get; private set; }

		public virtual void Send(RabbitMessage message, RabbitAddress address)
		{
			this.ThrowWhenDisposed();

			var properties = this.channel.CreateBasicProperties();
			properties.AppId = message.ProducerId;
			properties.ContentEncoding = message.ContentEncoding;
			properties.ContentType = message.ContentType;
			properties.SetPersistent(message.Durable);

			properties.CorrelationId = message.CorrelationId;
			properties.Expiration = message.Expiration.ToUniversalTime().ToString(DateTimeFormat);

			properties.Headers = new Hashtable();
			foreach (var item in message.Headers ?? new Dictionary<string, string>())
				properties.Headers[item.Key] = item.Value;

			properties.MessageId = message.MessageId.ToString();
			properties.ReplyTo = message.ReplyTo;
			properties.Timestamp = new AmqpTimestamp(SystemTime.UtcNow.ToEpochTime());
			properties.Type = message.MessageType;

			this.channel.BasicPublish(address.Exchange, message.RoutingKey, properties, message.Body);
		}
		public virtual RabbitMessage Receive(TimeSpan timeout)
		{
			this.ThrowWhenDisposed();

			BasicDeliverEventArgs result;
			if (!this.subscription.Next((int)timeout.TotalMilliseconds, out result))
				return null;

			var properties = result.BasicProperties;
			var messageId = string.IsNullOrEmpty(properties.MessageId)
				? Guid.Empty : Guid.Parse(properties.MessageId);
			var expiration = string.IsNullOrEmpty(properties.Expiration)
				? DateTime.MaxValue : DateTime.Parse(properties.Expiration);

			return new RabbitMessage
			{
				MessageId = messageId,
				ProducerId = properties.AppId,
				Created = properties.Timestamp.UnixTime.ToDateTime(),
				MessageType = properties.Type,

				ContentEncoding = properties.ContentEncoding,
				ContentType = properties.ContentType,

				Durable = properties.DeliveryMode == 2,
				CorrelationId = properties.CorrelationId,
				Expiration = expiration,

				Headers = ParseHeaders(properties.Headers),

				ReplyTo = properties.ReplyTo, // TODO: convert to Uri?
				UserId = properties.UserId,

				DeliveryTag = result.DeliveryTag,
				DeliveryCount = result.Redelivered ? 1 : 0, // TODO: count # of deliveries
				SourceExchange = result.Exchange,
				RoutingKey = result.RoutingKey,
				Body = result.Body
			};
		}
		private static IDictionary<string, string> ParseHeaders(IDictionary value)
		{
			if (value == null)
				return null;

			return value.Cast<DictionaryEntry>()
				.Select(x => (string)x.Key)
				.ToDictionary(x => x, y => Encoding.UTF8.GetString((byte[])value[y]));
		}

		private void ThrowWhenDisposed()
		{
			if (this.disposed)
				throw new ObjectDisposedException(typeof(RabbitConnector).Name, "The object has already been disposed.");
		}

		public RabbitConnector(object connection, RabbitTransactionType transactionType)
			: this(connection as IConnection)
		{
			this.UnitOfWork = new RabbitUnitOfWork(this.channel, null, transactionType);
		}
		public RabbitConnector(object connection, RabbitTransactionType transactionType, RabbitAddress address)
			: this(connection as IConnection)
		{
			if (address == null)
				throw new ArgumentNullException("address");
			if (string.IsNullOrEmpty(address.Queue))
				throw new ArgumentException("The address provided does not indicate a queue.", "address");

			// a new subscription with RabbitTransactionType.None will cause the server to place
			// the configured number of messages (default prefetch=1) into the local channel buffer
			this.subscription = new Subscription(
				this.channel,
				address.Queue,
				transactionType == RabbitTransactionType.None);

			this.UnitOfWork = new RabbitUnitOfWork(this.channel, this.subscription, transactionType);
		}
		private RabbitConnector(IConnection connection)
		{
			if (connection == null)
				throw new ArgumentNullException("connection");

			// TODO: catch/dispose and rethrow wrapped exception
			this.channel = connection.CreateModel();
		}
		~RabbitConnector()
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
			if (this.disposed || !disposing)
				return;

			this.disposed = true;
			this.UnitOfWork.Dispose();
			this.channel.Dispose();
		}

		private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
		private readonly IModel channel;
		private readonly Subscription subscription;
		private bool disposed;
	}
}