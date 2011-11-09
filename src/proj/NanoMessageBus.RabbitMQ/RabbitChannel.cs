namespace NanoMessageBus.RabbitMQ
{
	using System;
	using System.Collections;
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

			var properties = this.GetProperties(channel, message);
			var payload = this.SerializePayload(message);
			var routingKey = GetRoutingKey(message);

			foreach (var exchange in recipients.Select(GetExchange))
				channel.BasicPublish(exchange, routingKey, properties, payload);
		}
		private IBasicProperties GetProperties(IModel channel, EnvelopeMessage message)
		{
			var properties = channel.CreateBasicProperties();

			properties.MessageId = message.MessageId.ToString();
			properties.ReplyTo = (message.ReturnAddress ?? new Uri(string.Empty)).ToString();
			properties.SetPersistent(message.Persistent);
			properties.ContentEncoding = string.Empty; // TODO
			properties.ContentType = this.serializer.ContentType;
			properties.Expiration = (SystemTime.UtcNow + message.TimeToLive).ToString();

			if (message.Headers.Count > 0)
				properties.Headers = properties.Headers ?? new Hashtable();

			foreach (var item in message.Headers)
				properties.Headers[item.Key] = item.Value;

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
		private static string GetRoutingKey(EnvelopeMessage message)
		{
			var first = message.LogicalMessages.First();
			return (first.GetType().FullName ?? string.Empty).ToLowerInvariant();
		}
		private static string GetExchange(Uri recipient)
		{
			return recipient.AbsolutePath.Substring(1); // remove leading slash
		}

		public virtual EnvelopeMessage Receive()
		{
			var timeout = TimeSpan.FromMilliseconds(500); // TODO: evaluate sleep timeout vs WaitOne
			var context = this.delivery();
			return context.Receive(timeout);
		}

		public RabbitChannel(
			Uri localAddress,
			Func<IModel> channelFactory,
			Func<DeliveryContext> delivery,
			ISerializer serializer)
		{
			this.EndpointAddress = localAddress;
			this.channelFactory = channelFactory;
			this.delivery = delivery;
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

		private readonly Func<IModel> channelFactory;
		private readonly Func<DeliveryContext> delivery;
		private readonly ISerializer serializer;
	}
}