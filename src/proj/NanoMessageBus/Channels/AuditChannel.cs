namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections.Generic;
	using Logging;

	public class AuditChannel : IMessagingChannel
	{
		public virtual string GroupName
		{
			get { return this.CurrentContext.GroupName; }
		}
		public virtual ChannelMessage CurrentMessage
		{
			get { return this.CurrentContext.CurrentMessage; }
		}
		public virtual IChannelTransaction CurrentTransaction
		{
			get { return this.CurrentContext.CurrentTransaction; }
		}
		public virtual IChannelGroupConfiguration CurrentConfiguration
		{
			get { return this.CurrentContext.CurrentConfiguration; }
		}
		public virtual IDependencyResolver CurrentResolver
		{
			get { return this.CurrentContext.CurrentResolver; }
		}
		protected virtual IDeliveryContext CurrentContext
		{
			get { return this.currentContext ?? this.channel; }
		}

		public virtual IDispatchContext PrepareDispatch(object message = null)
		{
			return this.channel.PrepareDispatch(message);
		}
		public virtual void Send(ChannelEnvelope envelope)
		{
			if (envelope == null)
				throw new ArgumentNullException("envelope");

			this.ThrowWhenDisposed();

			foreach (var listener in this.listeners)
				listener.Audit(envelope);

			this.channel.Send(envelope);
		}

		public virtual void BeginShutdown()
		{
			this.channel.BeginShutdown();
		}
		public virtual void Receive(Action<IDeliveryContext> callback)
		{
			this.channel.Receive(context => this.Receive(context, callback));
		}
		protected virtual void Receive(IDeliveryContext context, Action<IDeliveryContext> callback)
		{
			try
			{
				this.currentContext = context;
				callback(this);
			}
			finally
			{
				this.currentContext = null;
			}
		}

		protected virtual void ThrowWhenDisposed()
		{
			if (!this.disposed)
				return;

			Log.Warn("The channel has been disposed.");
			throw new ObjectDisposedException(typeof(AuditChannel).Name);
		}

		public AuditChannel(IMessagingChannel channel, ICollection<IAuditListener> listeners)
		{
			if (channel == null)
				throw new ArgumentNullException("channel");

			if (listeners == null)
				throw new ArgumentNullException("listeners");

			if (listeners.Count == 0)
				throw new ArgumentException("At least one audit listener must be provided.", "listeners");

			this.channel = channel;
			this.listeners = listeners;
		}
		~AuditChannel()
		{
			this.Dispose(false);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
				return;

			this.disposed = true;

			foreach (var listener in this.listeners)
				listener.Dispose();

			this.channel.Dispose();
		}

		private static readonly ILog Log = LogFactory.Build(typeof(AuditChannel)); // TODO
		private readonly IMessagingChannel channel;
		private readonly ICollection<IAuditListener> listeners;
		private IDeliveryContext currentContext;
		private bool disposed;
	}
}