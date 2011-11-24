namespace NanoMessageBus.RabbitChannel
{
	using System;
	using RabbitMQ.Client.Events;

	public class RabbitSubscription : IDisposable
	{
		public virtual void BeginReceive<T>(TimeSpan timeout, Action<T> callback) where T : class
		{
			if (timeout < TimeSpan.Zero)
				throw new ArgumentException("The timespan must be positive.", "timeout");
			if (callback == null)
				throw new ArgumentNullException("callback");

			this.ThrowWhenDisposed();

			while (!this.disposed)
			{
				var delivery = this.adapter.BeginReceive<T>(timeout);
				if (delivery != null)
					callback(delivery);	
			}
		}
		public virtual void AcknowledgeMessage()
		{
			this.ThrowWhenDisposed();
			this.adapter.AcknowledgeMessage();
		}
		public virtual void RetryMessage<T>(T message) where T : class
		{
			if (message == null)
				throw new ArgumentNullException("message");

			var retry = message as BasicDeliverEventArgs;
			if (retry == null)
				throw new ArgumentException("The message must be of type 'BasicDeliverEventArgs'.", "message");

			this.ThrowWhenDisposed();
			this.adapter.RetryMessage(retry); // TODO: try/catch shutdown?
		}

		protected virtual void ThrowWhenDisposed()
		{
			if (this.disposed)
				throw new ObjectDisposedException("RabbitSubscription");
		}

		public RabbitSubscription(SubscriptionAdapter adapter)
		{
			this.adapter = adapter;
		}
		protected RabbitSubscription()
		{
		}
		~RabbitSubscription()
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
			if (!disposing || this.disposed)
				return;

			this.disposed = true;
			this.adapter.Dispose();
		}

		private readonly SubscriptionAdapter adapter;
		private bool disposed;
	}
}