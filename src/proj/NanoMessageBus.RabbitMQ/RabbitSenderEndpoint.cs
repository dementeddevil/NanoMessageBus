namespace NanoMessageBus.RabbitMQ
{
	using System;
	using System.Linq;
	using Endpoints;
	using Serialization;

	public class RabbitSenderEndpoint : ISendToEndpoints
	{
		//// TODO: logging

		public virtual void Send(EnvelopeMessage message, params Uri[] recipients)
		{
			recipients = recipients ?? new Uri[0];
			if (recipients.Length == 0)
				return;

			var connector = this.connectorFactory(); // TODO: catch connection errors

			var pending = new RabbitMessage
			{
				MessageId = message.MessageId,
				ProducerId = string.Empty, // TODO?
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

			foreach (var address in recipients.Select(x => new RabbitAddress(x)))
				connector.Send(pending, address);
		}

		public RabbitSenderEndpoint(Func<RabbitConnector> connectorFactory, ISerializer serializer)
		{
			this.connectorFactory = connectorFactory;
			this.serializer = serializer;
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

		private readonly Func<RabbitConnector> connectorFactory;
		private readonly ISerializer serializer;
	}
}