namespace NanoMessageBus.RabbitMQ
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Runtime.Serialization;
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
				return this.ForwardToDeadLetterExchange();

			var properties = this.delivery.BasicProperties;

			var messages = this.TryDeserialize();
			if (messages == null)
				return null;

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

		private EnvelopeMessage ForwardToDeadLetterExchange()
		{
			var exchange = this.deadLetterExchange.AbsolutePath.Substring(1); // remove leading / character
			var properties = (IBasicProperties)this.delivery.BasicProperties.Clone();
			this.channel.BasicPublish(
				exchange, this.delivery.RoutingKey, properties, this.delivery.Body);

			return null;
		}
		private void ForwardToPoisonMessageExchange(Exception exception)
		{
			// TODO: append exception to headers
			var exchange = this.poisonMessageExchange.AbsolutePath.Substring(1); // remove leading / character
			var properties = (IBasicProperties)this.delivery.BasicProperties.Clone();

			this.channel.BasicPublish(
				exchange, this.delivery.RoutingKey, properties, this.delivery.Body);
		}

		private ICollection<object> TryDeserialize()
		{
			try
			{
				var graph = this.Deserialize();
				return (ICollection<object>)graph;
			}
			catch (InvalidCastException e)
			{
				this.ForwardToPoisonMessageExchange(e);
			}
			catch (SerializationException e)
			{
				this.ForwardToPoisonMessageExchange(e);
			}

			return null;
		}

		private object Deserialize()
		{
			var deserializer = this.serializer(this.delivery.BasicProperties.ContentType);
			using (var stream = new MemoryStream(this.delivery.Body))
				return deserializer.Deserialize(stream);
		}

		public virtual void AcknowledgeDelivery()
		{
			if (this.delivery != null && this.acknowledge)
				this.subscription.Ack(this.delivery);
		}

		public DeliveryContext(
			Func<string, ISerializer> serializer,
			IModel channel,
			Uri localAddress,
			Uri deadLetterExchange,
			Uri poisonMessageExchange,
			bool acknowledge)
		{
			var queue = localAddress.AbsolutePath.Substring(1); // remove leading / character

			this.channel = channel;
			this.serializer = serializer;
			this.subscription = new Subscription(channel, queue, !acknowledge);
			this.acknowledge = acknowledge;

			this.deadLetterExchange = deadLetterExchange;
			this.poisonMessageExchange = poisonMessageExchange;
		}

		private readonly IModel channel;
		private readonly Func<string, ISerializer> serializer;
		private readonly Subscription subscription;
		private readonly bool acknowledge;
		private readonly Uri deadLetterExchange;
		private readonly Uri poisonMessageExchange;

		private BasicDeliverEventArgs delivery;
	}
}