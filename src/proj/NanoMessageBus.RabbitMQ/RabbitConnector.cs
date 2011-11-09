namespace NanoMessageBus.RabbitMQ
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using global::RabbitMQ.Client;
	using global::RabbitMQ.Client.Events;
	using global::RabbitMQ.Client.MessagePatterns;
	
	public class RabbitConnector : IDisposable
	{
		//// TODO: logging
		//// TODO: we need to be sure we apply appropriate try/catch semantics here (if channel unavailable/connection lost)

		public Uri EndpointAddress { get; private set; }

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

		public virtual void AcknowledgeReceipt()
		{
			this.ThrowWhenDisposed();
			this.subscription.Ack();
		}
		public virtual void BeginTransaction()
		{
			this.ThrowWhenDisposed();
			this.channel.TxSelect();
		}
		public virtual void CommitTransaction()
		{
			this.ThrowWhenDisposed();
			this.channel.TxCommit();
		}
		public virtual void RollbackTransaction()
		{
			this.ThrowWhenDisposed();
			this.channel.TxRollback();
		}

		public static RabbitConnector OpenSend(object channel, RabbitAddress address)
		{
			return new RabbitConnector(channel as IModel, null, address);
		}
		public static RabbitConnector OpenReceive(object channel, RabbitAddress address, bool acknowledge)
		{
			var model = channel as IModel; // TODO: catch/dispose and rethrow wrapped exception
			var subscription = new Subscription(model, address.Queue, !acknowledge);

			return new RabbitConnector(model, subscription, address);
		}

		private void ThrowWhenDisposed()
		{
			if (this.disposed)
				throw new ObjectDisposedException(typeof(RabbitConnector).Name, "The object has already been disposed.");
		}

		private RabbitConnector(IModel channel, Subscription subscription, RabbitAddress address)
		{
			this.channel = channel;
			this.subscription = subscription;
			this.EndpointAddress = address.Raw;
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
			this.channel.Dispose();
		}

		private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
		private readonly IModel channel;
		private readonly Subscription subscription;
		private bool disposed;
	}
}