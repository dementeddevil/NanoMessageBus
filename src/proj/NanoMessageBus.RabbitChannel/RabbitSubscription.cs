namespace NanoMessageBus.RabbitChannel
{
	using System;
	using Logging;
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
				if (!callback(this.inner.BeginReceive(timeout)))
					return;
		}

		public virtual void AcknowledgeMessages()
		{
			this.ThrowWhenDisposed();
			this.inner.AcknowledgeMessages();
		}

		public RabbitSubscription(Subscription inner)
		{
			this.inner = inner;
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
			this.inner.Dispose();
		}

		private static readonly ILog Log = LogFactory.Builder(typeof(RabbitSubscription));
		private readonly Subscription inner;
		private bool disposed;
	}
}