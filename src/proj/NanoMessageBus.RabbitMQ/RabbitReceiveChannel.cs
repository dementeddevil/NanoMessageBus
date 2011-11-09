namespace NanoMessageBus.RabbitMQ
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Runtime.Serialization;
	using Endpoints;
	using Serialization;

	public class RabbitReceiveChannel : IReceiveFromEndpoints
	{
		//// TODO: logging

		public Uri EndpointAddress { get; private set; }
		public EnvelopeMessage Receive()
		{
			// TODO: catch connection errors
			var connector = this.connectorFactory();
			var message = connector.Receive(DefaultReceiveWait);
			if (message == null)
				return null;

			if (this.ForwardWhenExpired(message))
				return null;

			var logicalMessages = this.TryDeserialize(message);
			if (logicalMessages == null)
				return null; // message cannot be deserialized

			// TODO: reply-to address and TTL
			return new EnvelopeMessage(
				message.MessageId,
				null,
				TimeSpan.MaxValue,
				message.Durable,
				message.Headers,
				logicalMessages);
		}
		private ICollection<object> TryDeserialize(RabbitMessage message)
		{
			try
			{
				return (ICollection<object>)this.Deserialize(message);
			}
			catch (SerializationException e)
			{
				this.ForwardWhenPoison(message, e);
			}
			catch (InvalidCastException e)
			{
				this.ForwardWhenPoison(message, e);
			}

			return null;
		}
		private object Deserialize(RabbitMessage message)
		{
			var serializer = this.serializerFactory(message.ContentType);
			using (var stream = new MemoryStream(message.Body))
				return serializer.Deserialize(stream);
		}

		private bool ForwardWhenExpired(RabbitMessage message)
		{
			if (message.Expiration >= SystemTime.UtcNow)
				return false;

			return this.Forward(message, this.deadLetterAddress);
		}
		private void ForwardWhenPoison(RabbitMessage message, Exception exception)
		{
			// TODO: add exception info to the message
			var connector = this.connectorFactory();
			connector.Send(message, this.poisonMessageAddress);
		}
		private bool Forward(RabbitMessage message, RabbitAddress address)
		{
			var connector = this.connectorFactory();
			connector.Send(message, address);
			return true;
		}

		public RabbitReceiveChannel(
			Uri localAddress,
			Uri deadLetterAddress,
			Uri poisonMessageAddress,
			Func<RabbitConnector> connectorFactory,
			Func<string, ISerializer> serializerFactory)
		{
			this.EndpointAddress = localAddress;
			this.deadLetterAddress = new RabbitAddress(deadLetterAddress);
			this.poisonMessageAddress = new RabbitAddress(poisonMessageAddress);
			this.connectorFactory = connectorFactory;
			this.serializerFactory = serializerFactory;
		}
		~RabbitReceiveChannel()
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

		private static readonly TimeSpan DefaultReceiveWait = TimeSpan.FromMilliseconds(500);
		private readonly RabbitAddress deadLetterAddress;
		private readonly RabbitAddress poisonMessageAddress;
		private readonly Func<RabbitConnector> connectorFactory;
		private readonly Func<string, ISerializer> serializerFactory;
	}
}