namespace NanoMessageBus.RabbitChannel
{
	using System;
	using RabbitMQ.Client.Events;
	using RabbitMQ.Client.Exceptions;

	public class RabbitSubscription : IDisposable
	{
		public virtual void Receive(TimeSpan timeout, Func<BasicDeliverEventArgs, bool> callback)
		{
			if (timeout < TimeSpan.Zero)
				throw new ArgumentException("The time span must be positive.", "timeout");
			if (callback == null)
				throw new ArgumentNullException("callback");

			this.ThrowWhenDisposed();
			this.TryReceive(timeout, callback);
		}
		protected virtual void ThrowWhenDisposed()
		{
			if (this.disposed)
				throw new ObjectDisposedException(typeof(RabbitSubscription).Name);
		}
		protected virtual void TryReceive(TimeSpan timeout, Func<BasicDeliverEventArgs, bool> callback)
		{
			try
			{
				this.PerformReceive(timeout, callback);
			}
			catch (OperationInterruptedException e)
			{
				throw new ChannelConnectionException(e.Message, e);
			}
		}
		protected virtual void PerformReceive(TimeSpan timeout, Func<BasicDeliverEventArgs, bool> callback)
		{
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