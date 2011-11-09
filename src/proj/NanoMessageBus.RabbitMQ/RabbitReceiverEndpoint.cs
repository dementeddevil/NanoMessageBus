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
		// TODO: logging
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

			this.connectorFactory().Send(message, this.deadLetterExchange);
			return true;
		}
		private void ForwardWhenPoison(RabbitMessage message, Exception exception)
		{
			AppendException(message, exception, 0);
			this.connectorFactory().Send(message, this.poisonMessageExchange);
		}

		public bool IsPoison(EnvelopeMessage message)
		{
			var connector = this.connectorFactory().CurrentMessage;
			return this.IsPoison(connector.RetryCount);
		}
		public void ClearFailures(EnvelopeMessage message)
		{
		}
		public void HandleFailure(EnvelopeMessage message, Exception exception)
		{
			var connector = this.connectorFactory();
			connector.UnitOfWork.Clear(); // don't perform any dispatch operations, e.g. publish/send/etc

			var current = AppendException(connector.CurrentMessage, exception, 0);
			var destination = this.GetMessageDestination(current);
			connector.Send(current, destination);

			connector.UnitOfWork.Complete(); // but still remove the incoming poison message from the queue
		}

		private static RabbitMessage AppendException(RabbitMessage message, Exception exception, int depth)
		{
			if (exception == null)
				return message;

			message.Headers[ExceptionHeader.FormatWith(depth, "type")] = exception.GetType().FullName;
			message.Headers[ExceptionHeader.FormatWith(depth, "message")] = exception.Message;
			message.Headers[ExceptionHeader.FormatWith(depth, "stack")] = exception.StackTrace;

			return AppendException(message, exception.InnerException, depth + 1);
		}
		private RabbitAddress GetMessageDestination(RabbitMessage message)
		{
			if (this.IsPoison(++message.RetryCount))
				return this.poisonMessageExchange;

			return new RabbitAddress("/" + message.SourceExchange); // return to exchange for retry...
		}
		private bool IsPoison(int failures)
		{
			return this.maxAttempts >= failures;
		}

		public RabbitReceiverEndpoint(
			Func<RabbitConnector> connectorFactory,
			RabbitAddress deadLetterExchange,
			RabbitAddress poisonMessageExchange,
			Func<string, ISerializer> serializerFactory,
			int maxAttempts)
		{
			this.connectorFactory = connectorFactory;
			this.deadLetterExchange = deadLetterExchange;
			this.poisonMessageExchange = poisonMessageExchange;
			this.serializerFactory = serializerFactory;
			this.maxAttempts = maxAttempts;
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

		private const string ExceptionHeader = "x-exception.{0}-{1}";
		private static readonly TimeSpan DefaultReceiveWait = TimeSpan.FromMilliseconds(500);
		private readonly Func<RabbitConnector> connectorFactory;
		private readonly RabbitAddress deadLetterExchange;
		private readonly RabbitAddress poisonMessageExchange;
		private readonly Func<string, ISerializer> serializerFactory;
		private readonly int maxAttempts;
	}
}