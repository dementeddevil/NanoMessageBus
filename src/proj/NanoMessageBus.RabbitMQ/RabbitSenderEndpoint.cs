namespace NanoMessageBus.RabbitMQ
{
	using System;
	using System.Linq;
	using Endpoints;
	using Serialization;

	public class RabbitSenderEndpoint : ISendToEndpoints
	{
		public string ProducerId { get; set; }

		public virtual void Send(EnvelopeMessage message, params Uri[] recipients)
		{
			recipients = recipients ?? new Uri[0];
			if (recipients.Length == 0)
				return;

			var pending = this.BuildMessage(message);
			var channel = this.connector.Current;
			foreach (var exchange in recipients.Select(x => new RabbitAddress(x).Exchange))
				channel.Send(pending, exchange);
		}
		private RabbitMessage BuildMessage(EnvelopeMessage message)
		{
			return new RabbitMessage
			{
				MessageId = message.MessageId,
				ProducerId = this.ProducerId,
				CorrelationId = message.CorrelationId,
				ContentType = this.serializer.ContentType,
				ContentEncoding = this.serializer.ContentEncoding,
				Durable = message.Persistent,
				Expiration = message.Expiration(),
				MessageType = message.MessageType(),
				ReplyTo = message.ReturnAddress.ToString(), // TODO
				RoutingKey = message.RoutingKey(),
				Headers = message.Headers,
				Body = message.Serialize(this.serializer),
			};
		}

		public RabbitSenderEndpoint(RabbitConnector connector, ISerializer serializer)
		{
			this.connector = connector;
			this.serializer = serializer;
			this.ProducerId = string.Empty;
		}
		~RabbitSenderEndpoint()
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
		}

		private readonly RabbitConnector connector;
		private readonly ISerializer serializer;
	}
}