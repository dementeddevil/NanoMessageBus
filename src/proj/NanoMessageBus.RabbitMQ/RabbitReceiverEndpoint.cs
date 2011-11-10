namespace NanoMessageBus.RabbitMQ
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Runtime.Serialization;
	using Endpoints;
	using Serialization;

	public class RabbitReceiverEndpoint : IReceiveFromEndpoints
	{
		public virtual EnvelopeMessage Receive()
		{
			// TODO: catch connection errors
			var message = this.connectorFactory().Receive(DefaultReceiveWait);
			if (message == null)
				return null;

			if (this.ForwardWhenExpired(message))
				return null;

			var logicalMessages = this.TryDeserialize(message);
			if (logicalMessages == null)
				return null; // message cannot be deserialized

			return new EnvelopeMessage(
				message.MessageId,
				message.CorrelationId,
				null, // TODO
				TimeSpan.MaxValue, // TODO
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
				this.poisonMessageHandler.ForwardToPoisonMessageExchange(message, e);
			}
			catch (InvalidCastException e)
			{
				this.poisonMessageHandler.ForwardToPoisonMessageExchange(message, e);
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

			this.poisonMessageHandler.ForwardToDeadLetterExchange(message);
			return true;
		}

		public RabbitReceiverEndpoint(
			Func<RabbitConnector> connectorFactory,
			RabbitPoisonMessageHandler poisonMessageHandler,
			Func<string, ISerializer> serializerFactory)
		{
			this.connectorFactory = connectorFactory;
			this.serializerFactory = serializerFactory;
			this.poisonMessageHandler = poisonMessageHandler;
		}
		~RabbitReceiverEndpoint()
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
		private readonly Func<RabbitConnector> connectorFactory;
		private readonly RabbitPoisonMessageHandler poisonMessageHandler;
		private readonly Func<string, ISerializer> serializerFactory;
	}
}