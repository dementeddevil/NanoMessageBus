namespace NanoMessageBus.RabbitMQ
{
	using System;
	using System.IO;
	using System.Linq;
	using Endpoints;
	using global::RabbitMQ.Client;
	using Serialization;

	public class RabbitChannel : ISendToEndpoints, IReceiveFromEndpoints
	{
		public virtual Uri EndpointAddress { get; private set; }

		public virtual void Send(EnvelopeMessage message, params Uri[] recipients)
		{
			var channel = this.channelFactory();

			var properties = GetProperties(channel, message);
			var payload = this.SerializePayload(message);
			var type = message.LogicalMessages.First().GetType();

			foreach (var address in recipients.Select(recipient => GetAddress(recipient, type)))
				channel.BasicPublish(address, properties, payload);
		}
		private static IBasicProperties GetProperties(IModel channel, EnvelopeMessage message)
		{
			var properties = channel.CreateBasicProperties();

			properties.MessageId = message.MessageId.ToString();
			properties.ReplyTo = (message.ReturnAddress ?? new Uri(string.Empty)).ToString();
			properties.SetPersistent(message.Persistent);
			properties.ContentEncoding = string.Empty; // TODO
			properties.ContentType = "application/json"; // TODO: determined by the serializer
			properties.Expiration = (DateTime.UtcNow + message.TimeToLive).ToString(); // TODO

			foreach (var item in message.Headers)
				properties.Headers[EnvelopeHeader + item.Key] = item.Value;

			return properties;
		}
		private byte[] SerializePayload(EnvelopeMessage message)
		{
			using (var stream = new MemoryStream())
			{
				this.serializer.Serialize(stream, message.LogicalMessages);
				return stream.ToArray();
			}
		}
		private static PublicationAddress GetAddress(Uri recipient, Type primaryMessage)
		{
			// TODO: if specified, use DescriptionAtribute for metadata for routing key and for exchange...
			return new PublicationAddress(string.Empty, recipient.Host, primaryMessage.FullName);
		}

		public virtual EnvelopeMessage Receive()
		{
			// TODO: where to ack?
			return null;
		}

		public RabbitChannel(Uri address, Func<IModel> channelFactory, ISerializer serializer)
		{
			this.EndpointAddress = address;
			this.channelFactory = channelFactory;
			this.serializer = serializer;
		}
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
		}

		private const string EnvelopeHeader = "x-envelope-";
		private readonly ISerializer serializer;
		private readonly Func<IModel> channelFactory;
	}
}