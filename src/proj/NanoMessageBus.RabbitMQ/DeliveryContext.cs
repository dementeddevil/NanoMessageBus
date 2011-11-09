namespace NanoMessageBus.RabbitMQ
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
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

			if (this.IsExpired())
				return null;

			var properties = this.delivery.BasicProperties;
			var messages = this.Deserialize();
			var headers = this.GetHeaders();

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
		private IDictionary<string, string> GetHeaders()
		{
			var source = this.delivery.BasicProperties.Headers;
			if (source == null)
				return null;

			return source.Cast<DictionaryEntry>()
				.Select(x => (string)x.Key)
				.ToDictionary(x => x, y => Encoding.UTF8.GetString((byte[])source[y]));
		}
		private bool IsExpired()
		{
			var expiration = this.delivery.BasicProperties.Expiration;
			if (string.IsNullOrEmpty(expiration))
				return false;

			return SystemTime.UtcNow > DateTime.Parse(expiration);
		}

		private ICollection<object> Deserialize()
		{
			var deserializer = this.serializer(this.delivery.BasicProperties.ContentType);
			using (var stream = new MemoryStream(this.delivery.Body))
				return (ICollection<object>)deserializer.Deserialize(stream);
		}

		public virtual void AcknowledgeDelivery()
		{
			if (this.delivery != null && this.acknowledge)
				this.subscription.Ack(this.delivery);
		}

		public DeliveryContext(Func<string, ISerializer> serializer, IModel channel, Uri queueAddress, bool acknowledge)
		{
			var queue = queueAddress.AbsolutePath.Substring(1); // remove leading / character

			this.serializer = serializer;
			this.subscription = new Subscription(channel, queue, !acknowledge);
			this.acknowledge = acknowledge;
		}

		private readonly Func<string, ISerializer> serializer;
		private readonly Subscription subscription;
		private readonly bool acknowledge;
		private BasicDeliverEventArgs delivery;
	}
}