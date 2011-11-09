namespace NanoMessageBus.RabbitMQ
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using global::RabbitMQ.Client;
	using global::RabbitMQ.Client.Events;
	using global::RabbitMQ.Client.MessagePatterns;
	using Serialization;

	public class DeliveryContext
	{
		public virtual EnvelopeMessage Receive(TimeSpan timeout)
		{
			if (!this.subscription.Next(timeout.Milliseconds, out this.delivery))
				return null;

			var messages = this.Deserialize();
			var properties = this.delivery.BasicProperties;

			var headers = new Dictionary<string, string>();

			var keys = properties.Headers.Keys.Cast<string>().Where(key => key.StartsWith(EnvelopeHeader));
			foreach (var key in keys)
				headers[key.Substring(0, EnvelopeHeader.Length)] = (string)properties.Headers[key];

			// TODO: get TTL from attributes of first logical message
			// TODO: get return address
			return new EnvelopeMessage(
				Guid.Parse(properties.MessageId),
				null,
				TimeSpan.MaxValue,
				properties.DeliveryMode == 0x2,
				headers,
				messages);
		}
		private ICollection<object> Deserialize()
		{
			// TODO: deserialization/casting failures result in forwarding the message to a poison message exchange
			// TODO: decide serializer based upon content type
			using (var stream = new MemoryStream(this.delivery.Body))
				return (ICollection<object>)this.serializer.Deserialize(stream);
		}

		public virtual void AcknowledgeDelivery()
		{
			if (this.delivery != null && this.acknowledge)
				this.subscription.Ack(this.delivery);
		}

		public DeliveryContext(ISerializer serializer, IModel channel, Uri queue, bool acknowledge)
		{
			this.serializer = serializer;
			this.subscription = new Subscription(channel, queue.Host, !this.acknowledge);
			this.acknowledge = acknowledge;
		}

		private const string EnvelopeHeader = "x-envelope-";
		private readonly ISerializer serializer;
		private readonly Subscription subscription;
		private readonly bool acknowledge;
		private BasicDeliverEventArgs delivery;
	}
}