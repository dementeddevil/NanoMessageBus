namespace NanoMessageBus.Channels
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
				Log.Verbose("Starting channel message subscription.");
				this.PerformReceive(timeout, callback);
			}
			catch (OperationInterruptedException e)
			{
				Log.Debug("Channel operation interrupted, aborting receive.");
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

			Log.Verbose("Disposing channel message subscription.");

			this.disposed = true;

			this.inner.TryDispose();
			Log.Debug("Channel message subscription disposed.");
		}

		private static readonly ILog Log = LogFactory.Build(typeof(RabbitSubscription));
		private readonly Subscription inner;
		private bool disposed;
	}
}