namespace NanoMessageBus.RabbitChannel
{
	using System;
	using RabbitMQ.Client;
	using RabbitMQ.Client.Events;
	using RabbitMQ.Client.MessagePatterns;

	public class RabbitSubscription
	{
		public virtual void BeginReceive(TimeSpan timeout, Action<RabbitMessage> callback)
		{
			if (timeout < TimeSpan.Zero)
				throw new ArgumentException("The timespan must be positive", "timeout");

			if (callback == null)
				throw new ArgumentNullException("callback");

			var milliseconds = (int)timeout.TotalMilliseconds;

			BasicDeliverEventArgs delivery;
			if (this.subscription.Next(milliseconds, out delivery) && delivery != null)
				callback(new RabbitMessage(delivery)); // make this an event-based consumer
		}
		public virtual void AcknowledgeReceipt()
		{
			this.subscription.Ack();
		}

		public RabbitSubscription(IModel channel, string queueName, RabbitTransactionType transactionType)
		{
			this.subscription = new Subscription(
				channel, queueName, transactionType == RabbitTransactionType.None);
		}
		protected RabbitSubscription()
		{
		}

		private readonly Subscription subscription;
	}
}