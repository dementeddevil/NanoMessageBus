﻿namespace NanoMessageBus.Channels
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
			try
			{
				Log.Verbose("Starting channel message subscription.");
				this.PerformReceive(timeout, callback);
			}
			catch (ChannelConnectionException)
			{
				throw;
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

			try
			{
				this.inner.Dispose();
			}
			catch (IOException)
			{
				Log.Debug("Unable to cleanly dispose subscription; operation failed.");
			}
			catch (OperationInterruptedException)
			{
				Log.Debug("Unable to cleanly dispose subscription; operation interrupted.");
			}
			catch (NotSupportedException)
			{
				Log.Debug("Unable to cleanly dispose subscription; multi-threaded shutdown.");
			}
			catch (Exception e)
			{
				Log.Error("Unable to cleanly dispose subscription.", e);
			}
			finally
			{
				Log.Debug("Channel message subscription disposed.");
			}
		}

		private static readonly ILog Log = LogFactory.Build(typeof(RabbitSubscription));
		private readonly Subscription inner;
		private bool disposed;
	}
}