namespace NanoMessageBus.RabbitMQ
{
	using System;
	using Handlers;

	public class RabbitPoisonMessageHandler : IHandlePoisonMessages
	{
		public virtual bool IsPoison(EnvelopeMessage message)
		{
			return this.IsPoison(message.FailureCount());
		}
		public virtual void ClearFailures(EnvelopeMessage message)
		{
		}
		public virtual void HandleFailure(EnvelopeMessage message, Exception exception)
		{
			this.unitOfWork.Clear(); // don't perform any dispatch operations

			var failures = message.FailureCount() + 1;
			message.Headers[RabbitKeys.FailureCount] = failures.ToString();
			var destination = this.IsPoison(failures)
				? this.poisonMessageExchange.Raw
				: new Uri("/" + message.Headers[RabbitKeys.SourceExchange]);
			this.sender.Send(message, destination);

			this.unitOfWork.Complete(); // but still remove the incoming poison message from the queue
		}
		private bool IsPoison(int failures)
		{
			return this.maxAttempts >= failures;
		}

		public RabbitPoisonMessageHandler(
			RabbitSenderEndpoint sender,
			RabbitAddress poisonMessageExchange,
			IHandleUnitOfWork unitOfWork,
			int maxAttempts)
		{
			this.sender = sender;
			this.poisonMessageExchange = poisonMessageExchange;
			this.unitOfWork = unitOfWork;
			this.maxAttempts = maxAttempts;
		}

		private readonly RabbitSenderEndpoint sender;
		private readonly RabbitAddress poisonMessageExchange;
		private readonly IHandleUnitOfWork unitOfWork;
		private readonly int maxAttempts;
	}
}