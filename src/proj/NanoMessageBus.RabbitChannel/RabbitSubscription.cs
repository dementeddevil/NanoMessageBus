namespace NanoMessageBus.Channels
{
	using System;
	using System.IO;
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
			// TODO: make this throw EndOfStreamException and then catch with a ChannelConnectionException...
			// TODO: also, let's have some infrastructure code throw an unhandled exception
			// and see if we can get something to spin at 100% CPU and then determine how best to remedy
			// the situation--perhaps be trying to reconnect?
			try
			{
				Log.Verbose("Starting channel message subscription.");
				this.PerformReceive(timeout, callback);
			}
			catch (EndOfStreamException e)
			{
				Log.Info("Inconsistent receiving state of internal RabbitMQ 'SharedQueue', aborting receive.", e);
				throw new ChannelConnectionException(e.Message, e);
			}
			catch (OperationInterruptedException e)
			{
				Log.Debug("Channel operation interrupted, aborting receive.", e);
				throw new ChannelConnectionException(e.Message, e);
			}
			catch (Exception e)
			{
				Log.Warn("Unexpected exception thrown.", e);
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