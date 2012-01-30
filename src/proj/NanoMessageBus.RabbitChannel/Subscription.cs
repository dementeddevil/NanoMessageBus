namespace NanoMessageBus.RabbitChannel
{
	using System;
	using RabbitMQ.Client;
	using RabbitMQ.Client.Events;

	public class Subscription : IDisposable
	{
		public virtual BasicDeliverEventArgs BeginReceive(TimeSpan timeout)
		{
			BasicDeliverEventArgs delivery;
			this.subscription.Next((int)timeout.TotalMilliseconds, out delivery);
			return delivery;
		}
		public virtual void AcknowledgeMessages()
		{
			var delivery = this.subscription.LatestEvent;
			var tag = delivery == null ? 0 : delivery.DeliveryTag;
			this.channel.BasicAck(tag, true);
		}

		public Subscription(IModel channel, RabbitChannelGroupConfiguration config) : this()
		{
			this.channel = channel;
			this.subscription = new RabbitMQ.Client.MessagePatterns.Subscription(
				channel,
				config.InputQueue,
				config.TransactionType == RabbitTransactionType.None);
		}
		protected Subscription()
		{
		}

		public virtual void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
				this.subscription.Close();
		}

		private readonly RabbitMQ.Client.MessagePatterns.Subscription subscription;
		private readonly IModel channel;
	}
}