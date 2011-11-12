namespace NanoMessageBus.RabbitMQ
{
	using System;

	public class RabbitFaultedMessageHandler
	{
		public virtual void ForwardToDeadLetterExchange(RabbitChannel channel)
		{
			channel.Send(channel.CurrentMessage, this.deadLetterExchange.Exchange);
		}

		public virtual void HandleMessageFailure(RabbitChannel channel, Exception exception)
		{
			var unitOfWork = channel.UnitOfWork;

			unitOfWork.Clear(); // don't perform any registered operations, e.g. publish, send, etc.

			if (++channel.CurrentMessage.RetryCount >= this.maxAttempts)
				this.ForwardToPoisonMessageExchange(channel, exception);
			else
				ForwardToRetryExchange(channel);

			unitOfWork.Complete(); // but still remove the incoming physical message from the queue
		}
		public virtual void ForwardToPoisonMessageExchange(RabbitChannel channel, Exception exception)
		{
			var message = channel.CurrentMessage;
			this.AppendException(message, exception, 0);
			channel.Send(message, this.poisonMessageExchange.Exchange);
		}
		private void AppendException(RabbitMessage message, Exception exception, int depth)
		{
			if (exception == null)
				return;

			message.Headers[ExceptionHeader.FormatWith(depth, "type")] = exception.GetType().FullName;
			message.Headers[ExceptionHeader.FormatWith(depth, "message")] = exception.Message;
			message.Headers[ExceptionHeader.FormatWith(depth, "stack")] = exception.StackTrace;

			this.AppendException(message, exception.InnerException, depth + 1);
		}
		private static void ForwardToRetryExchange(RabbitChannel channel)
		{
			channel.Send(channel.CurrentMessage, null); // TODO;
		}

		public RabbitFaultedMessageHandler(
			RabbitAddress poisonMessageExchange, RabbitAddress deadLetterExchange, int maxAttempts)
		{
			this.poisonMessageExchange = poisonMessageExchange;
			this.deadLetterExchange = deadLetterExchange;
			this.maxAttempts = maxAttempts;
		}

		private const string ExceptionHeader = "x-exception.{0}-{1}";
		private readonly RabbitAddress poisonMessageExchange;
		private readonly RabbitAddress deadLetterExchange;
		private readonly int maxAttempts;
	}
}