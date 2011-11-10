namespace NanoMessageBus.RabbitMQ
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Runtime.Serialization;
	using Endpoints;
	using Handlers;
	using Serialization;

	public class RabbitReceiverEndpoint : IReceiveFromEndpoints, IHandlePoisonMessages
	{
		public virtual EnvelopeMessage Receive()
		{
			var message = this.connectorFactory().Receive(DefaultReceiveWait);
			if (message == null)
				return null;

			if (this.faultHandler().ForwardToDeadLetterExchange())
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
				this.faultHandler().ForwardToPoisonMessageExchange(e);
			}
			catch (InvalidCastException e)
			{
				this.faultHandler().ForwardToPoisonMessageExchange(e);
			}

			return null;
		}
		private object Deserialize(RabbitMessage message)
		{
			var serializer = this.serializerFactory(message.ContentType);
			using (var stream = new MemoryStream(message.Body))
				return serializer.Deserialize(stream);
		}

		public virtual bool IsPoison(EnvelopeMessage message)
		{
			return false;
		}
		public virtual void ClearFailures(EnvelopeMessage message)
		{
		}
		public virtual void HandleFailure(EnvelopeMessage message, Exception exception)
		{
			this.faultHandler().HandleMessageFailure(exception);
		}

		public RabbitReceiverEndpoint(
			Func<RabbitConnector> connectorFactory,
			Func<RabbitFaultedMessageHandler> faultHandler,
			Func<string, ISerializer> serializerFactory)
		{
			this.connectorFactory = connectorFactory;
			this.serializerFactory = serializerFactory;
			this.faultHandler = faultHandler;
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
		private readonly Func<RabbitFaultedMessageHandler> faultHandler;
		private readonly Func<string, ISerializer> serializerFactory;
	}
}