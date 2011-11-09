namespace NanoMessageBus.RabbitMQ
{
	using System;
	using Handlers;

	public class RabbitPoisonMessageHandler : IHandlePoisonMessages
	{
		public virtual bool IsPoison(EnvelopeMessage message)
		{
			return this.IsPoison(message.FailureCount(FailureCountHeader));
		}
		public virtual void ClearFailures(EnvelopeMessage message)
		{
		}
		public virtual void HandleFailure(EnvelopeMessage message, Exception exception)
		{
			this.unitOfWork.Clear(); // don't perform any dispatch operations

			var failures = message.FailureCount(FailureCountHeader) + 1;
			message.Headers[FailureCountHeader] = failures.ToString();

			var destination = this.IsPoison(failures) ? this.poisonMessageExchange : this.localMessageExchange;
			this.sender.Send(message, destination.Raw);

			this.unitOfWork.Complete(); // but still remove the incoming poison message from the queue
		}
		private bool IsPoison(int failures)
		{
			return this.maxAttempts >= failures;
		}

		public RabbitPoisonMessageHandler(
			RabbitSenderEndpoint sender,
			RabbitAddress localMessageExchange,
			RabbitAddress poisonMessageExchange,
			IHandleUnitOfWork unitOfWork,
			int maxAttempts)
		{
			this.sender = sender;
			this.localMessageExchange = localMessageExchange;
			this.poisonMessageExchange = poisonMessageExchange;
			this.unitOfWork = unitOfWork;
			this.maxAttempts = maxAttempts;
		}

		private const string FailureCountHeader = "x-failure-count";
		private readonly RabbitSenderEndpoint sender;
		private readonly RabbitAddress localMessageExchange;
		private readonly RabbitAddress poisonMessageExchange;
		private readonly IHandleUnitOfWork unitOfWork;
		private readonly int maxAttempts;
	}
}