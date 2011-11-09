namespace NanoMessageBus.RabbitMQ
{
	using System;
	using System.Collections.Generic;

	public class RabbitMessage
	{
		public Guid MessageId { get; set; }
		public DateTime Created { get; set; }
		public string MessageType { get; set; }
		public string ProducerId { get; set; }
		public string CorrelationId { get; set; }
		public string ContentEncoding { get; set; }
		public string ContentType { get; set; }
		public bool Durable { get; set; }
		public DateTime Expiration { get; set; }
		public IDictionary<string, string> Headers { get; set; }
		public string ReplyTo { get; set; }

		public object DeliveryTag { get; set; }
		public int RetryCount { get; set; }
		public string SourceExchange { get; set; }
		public string UserId { get; set; }

		public string RoutingKey { get; set; }
		public byte[] Body { get; set; }

		public RabbitMessage()
		{
			this.Created = SystemTime.UtcNow;
		}
	}
}