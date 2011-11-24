namespace NanoMessageBus.RabbitChannel
{
	using System;
	using RabbitMQ.Client;
	using RabbitMQ.Client.Events;
	using RabbitMQ.Client.MessagePatterns;
	using RabbitMQ.Util;

	public class SubscriptionAdapter : IDisposable
	{
		public virtual T BeginReceive<T>(TimeSpan timeout) where T : class
		{
			BasicDeliverEventArgs delivery;
			this.subscription.Next((int)timeout.TotalMilliseconds, out delivery);
			return delivery as T;
		}
		public virtual void AcknowledgeMessage()
		{
			this.subscription.Ack();
		}
		public virtual void RetryMessage(object message)
		{
			this.queue.Enqueue(message);
		}

		public SubscriptionAdapter(IModel channel, string queueName, bool ack) : this()
		{
			this.subscription = new Subscription(channel, queueName, !ack);
			this.queue = ((QueueingBasicConsumer)this.subscription.Consumer).Queue;
		}
		protected SubscriptionAdapter()
		{
		}
		~SubscriptionAdapter()
		{
			this.Dispose(false);
		}

		public virtual void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
				return;

			try
			{
				this.subscription.Close();
			}
			catch
			{
				return;
			}
		}

		private readonly Subscription subscription;
		private readonly SharedQueue queue;
	}
}