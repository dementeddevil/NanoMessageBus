namespace NanoMessageBus.RabbitChannel
{
	using System;
	using RabbitMQ.Client;
	using RabbitMQ.Client.Events;
	using RabbitMQ.Util;

	public class Subscription : IDisposable
	{
		public virtual BasicDeliverEventArgs BeginReceive(TimeSpan timeout)
		{
			BasicDeliverEventArgs delivery;
			this.subscription.Next((int)timeout.TotalMilliseconds, out delivery);
			return delivery;
		}
		public virtual void AcknowledgeMessage()
		{
			this.subscription.Ack();
		}
		public virtual void RetryMessage(BasicDeliverEventArgs message)
		{
			this.queue.Enqueue(message);
		}

		public Subscription(IModel channel, string queueName, bool ack) : this()
		{
			this.subscription = new RabbitMQ.Client.MessagePatterns.Subscription(channel, queueName, !ack);
			this.queue = ((QueueingBasicConsumer)this.subscription.Consumer).Queue;
		}
		protected Subscription()
		{
		}
		~Subscription()
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

		private readonly RabbitMQ.Client.MessagePatterns.Subscription subscription;
		private readonly SharedQueue queue;
	}
}