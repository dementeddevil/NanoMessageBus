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
		//// TODO: logging

		public virtual EnvelopeMessage Receive()
		{
			// TODO: catch connection errors
			var message = this.connector().Receive(DefaultReceiveWait);
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

			// TODO: catch connection errors
			this.connector().Send(message, this.deadLetterExchange);
			return true;
		}
		private void ForwardWhenPoison(RabbitMessage message, Exception exception)
		{
			if (AppendException(message, exception, string.Empty))
				AppendException(message, exception.InnerException, "Inner");

			// TODO: catch connection errors
			this.connector().Send(message, this.poisonMessageExchange);
		}
		private static bool AppendException(RabbitMessage message, Exception exception, string prefix)
		{
			if (exception == null)
				return false;

			message.Headers[prefix + "Exception.Type"] = exception.GetType().FullName;
			message.Headers[prefix + "Exception.Message"] = exception.Message;
			message.Headers[prefix + "Exception.StackTrace"] = exception.StackTrace;
			return true;
		}

		public RabbitReceiverEndpoint(
			Func<RabbitConnector> connector,
			RabbitAddress deadLetterExchange,
			RabbitAddress poisonMessageExchange,
			Func<string, ISerializer> serializerFactory)
		{
			this.connector = connector;
			this.deadLetterExchange = deadLetterExchange;
			this.poisonMessageExchange = poisonMessageExchange;
			this.serializerFactory = serializerFactory;
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
		private readonly Func<RabbitConnector> connector;
		private readonly RabbitAddress deadLetterExchange;
		private readonly RabbitAddress poisonMessageExchange;
		private readonly Func<string, ISerializer> serializerFactory;
	}
}