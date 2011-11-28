namespace NanoMessageBus.RabbitChannel
{
	using System;
	using RabbitMQ.Client.Events;

	public class RabbitSubscription : IDisposable
	{
		public virtual void BeginReceive(TimeSpan timeout, Func<BasicDeliverEventArgs, bool> callback)
		{
			if (timeout < TimeSpan.Zero)
				throw new ArgumentException("The timespan must be positive.", "timeout");
			if (callback == null)
				throw new ArgumentNullException("callback");

			this.ThrowWhenDisposed();

			while (!this.disposed)
				if (!callback(this.adapter.BeginReceive(timeout)))
					return;
		}
		public virtual void AcknowledgeMessage()
		{
			this.ThrowWhenDisposed();
			this.adapter.AcknowledgeMessage();
		}
		public virtual void RetryMessage(BasicDeliverEventArgs message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			this.ThrowWhenDisposed();
			this.adapter.RetryMessage(message);
		}

		protected virtual void ThrowWhenDisposed()
		{
			if (this.disposed)
				throw new ObjectDisposedException("RabbitSubscription");
		}

		public RabbitSubscription(Subscription adapter)
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

		private readonly Subscription adapter;
		private bool disposed;
	}
}