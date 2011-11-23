namespace NanoMessageBus.RabbitChannel
{
	using System;
	using RabbitMQ.Client.Events;
	using RabbitMQ.Client.MessagePatterns;

	public class RabbitSubscription
	{
		public virtual void AcknowledgeReceipt()
		{
			if (this.current != null)
				this.subscription.Ack(this.current.Delivery);

			this.current = null;
		}
		public virtual RabbitMessage Next(TimeSpan timeout)
		{
			this.current = null;

			BasicDeliverEventArgs delivery;
			if (!this.subscription.Next((int)timeout.TotalMilliseconds, out delivery))
				return null;

			if (delivery == null)
				return null; // even though delivery may technically succeed, it can still return null

			return this.current = new RabbitMessage(delivery);
		}

		public RabbitSubscription(Subscription subscription)
		{
			this.subscription = subscription;
		}
		protected RabbitSubscription()
		{
		}

		private readonly Subscription subscription;
		private RabbitMessage current;
	}
}