namespace NanoMessageBus.RabbitMQ
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using global::RabbitMQ.Client;
	using global::RabbitMQ.Client.Events;

	public class RabbitMessage
	{
		public Guid MessageId { get; set; }
		public DateTime Created { get; set; }
		public string MessageType { get; set; }
		public string ProducerId { get; set; }
		public Guid CorrelationId { get; set; }
		public string ContentEncoding { get; set; }
		public string ContentType { get; set; }
		public bool Durable { get; set; }
		public DateTime Expiration { get; set; }
		public IDictionary<string, string> Headers { get; set; }
		public string ReplyTo { get; set; }

		public int RetryCount { get; set; }
		public object DeliveryTag { get; private set; }
		public string SourceExchange { get; private set; }
		public string UserId { get; private set; }

		public string RoutingKey { get; set; }
		public byte[] Body { get; set; }

		internal IBasicProperties SerializeProperties(IModel channel)
		{
			var properties = channel.CreateBasicProperties();

			properties.MessageId = this.MessageId.ToString();
			properties.AppId = this.ProducerId ?? string.Empty;
			properties.ContentEncoding = this.ContentEncoding ?? string.Empty;
			properties.ContentType = this.ContentType ?? string.Empty;
			properties.SetPersistent(this.Durable);

			properties.CorrelationId = this.CorrelationId.ToNull() ?? string.Empty;
			properties.Expiration = this.Expiration.ToUniversalTime().ToString(DateTimeFormat);

			properties.Headers = new Hashtable();
			foreach (var item in this.Headers ?? new Dictionary<string, string>())
				properties.Headers[item.Key] = item.Value;

			if (this.RetryCount > 0)
				properties.Headers[FailureCount] = this.RetryCount;

			properties.ReplyTo = this.ReplyTo ?? string.Empty;
			properties.Timestamp = new AmqpTimestamp(SystemTime.UtcNow.ToEpochTime());
			properties.Type = this.MessageType ?? string.Empty;

			return properties;
		}

		public RabbitMessage()
		{
			this.MessageId = Guid.NewGuid();
			this.Created = SystemTime.UtcNow;
		}
		internal RabbitMessage(BasicDeliverEventArgs delivery)
			: this()
		{
			var properties = delivery.BasicProperties;
			var headers = ParseHeaders(properties.Headers);

			this.MessageId = properties.MessageId.ToGuid();
			this.ProducerId = properties.AppId;
			this.Created = properties.Timestamp.UnixTime.ToDateTime();
			this.MessageType = properties.Type;

			this.ContentEncoding = properties.ContentEncoding;
			this.ContentType = properties.ContentType;

			this.Durable = properties.DeliveryMode == DurableMessage;
			this.CorrelationId = properties.CorrelationId.ToGuid();
			this.Expiration = properties.Expiration.ToDateTime();

			this.Headers = headers;

			this.ReplyTo = properties.ReplyTo; // TODO: convert to Uri?
			this.UserId = properties.UserId;

			this.DeliveryTag = delivery.DeliveryTag;
			this.RetryCount = ComputeRetryCount(headers, delivery.Redelivered);
			this.SourceExchange = delivery.Exchange;
			this.RoutingKey = delivery.RoutingKey;
			this.Body = delivery.Body;
		}
		private static IDictionary<string, string> ParseHeaders(IDictionary value)
		{
			if (value == null)
				return null;

			return value.Cast<DictionaryEntry>()
				.Select(x => (string)x.Key)
				.ToDictionary(x => x, y => Encoding.UTF8.GetString((byte[])value[y]));
		}
		private static int ComputeRetryCount(IDictionary<string, string> headers, bool redelivered)
		{
			string value;
			if (headers != null && headers.TryGetValue(FailureCount, out value))
				return value.ToInt();

			return redelivered ? 1 : 0;
		}

		private const string FailureCount = "x-failure-count";
		private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
		private const int DurableMessage = 2;
	}
}