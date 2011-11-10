namespace NanoMessageBus.RabbitMQ
{
	using System;
	using Handlers;

	public class RabbitPoisonMessageHandler : IHandlePoisonMessages
	{
		public virtual bool IsPoison(EnvelopeMessage message)
		{
			var current = this.connectorFactory().CurrentMessage;
			return this.IsPoison(current.RetryCount);
		}
		public virtual void ClearFailures(EnvelopeMessage message)
		{
		}
		public virtual void HandleFailure(EnvelopeMessage message, Exception exception)
		{
			var connector = this.connectorFactory(); // connector is stateful
			connector.UnitOfWork.Clear(); // don't perform any registered operations, e.g. publish, send, etc.

			var current = connector.CurrentMessage;
			if (this.IsPoison(++current.RetryCount))
				this.ForwardToPoisonMessageExchange(current, exception);
			else
				this.ForwardToRetryExchange(current);

			connector.UnitOfWork.Complete(); // but still remove the incoming physical message from the queue
		}

		public virtual void ForwardToDeadLetterExchange(RabbitMessage message)
		{
			this.connectorFactory().Send(message, this.deadLetterExchange);
		}
		public virtual void ForwardToPoisonMessageExchange(RabbitMessage message, Exception exception)
		{
			AppendException(message, exception, 0);
			this.connectorFactory().Send(message, this.poisonMessageExchange);
		}
		private void ForwardToRetryExchange(RabbitMessage message)
		{
			this.connectorFactory().Send(message, null); // TODO
		}

		private bool IsPoison(int failures)
		{
			return this.maxAttempts >= failures;
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

		public RabbitPoisonMessageHandler(
			Func<RabbitConnector> connectorFactory,
			RabbitAddress poisonMessageExchange,
			RabbitAddress deadLetterExchange,
			int maxAttempts)
		{
			this.connectorFactory = connectorFactory;
			this.poisonMessageExchange = poisonMessageExchange;
			this.deadLetterExchange = deadLetterExchange;
			this.maxAttempts = maxAttempts;
		}

		private const string ExceptionHeader = "x-exception.{0}-{1}";
		private readonly Func<RabbitConnector> connectorFactory;
		private readonly RabbitAddress poisonMessageExchange;
		private readonly RabbitAddress deadLetterExchange;
		private readonly int maxAttempts;
	}
}