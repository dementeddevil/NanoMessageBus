namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections.Generic;
	using Logging;

	public class AuditChannel : IMessagingChannel
	{
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
			Log.Debug("Preparing a dispatch");

			var context = new DefaultDispatchContext(this);
			return message == null ? context : context.WithMessage(message);
		}
		public virtual void Send(ChannelEnvelope envelope)
		{
			if (envelope == null)
				throw new ArgumentNullException("envelope");

			this.ThrowWhenDisposed();

			var messageId = envelope.MessageId();
			foreach (var auditor in this.auditors)
			{
				Log.Debug("Providing envelope '{0}' for inspection to auditor of type '{1}'.", messageId, auditor.GetType());
				auditor.AuditSend(envelope);
			}

			Log.Verbose("Sending envelope '{0}' through the underlying channel.", messageId);
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

		public AuditChannel(IMessagingChannel channel, ICollection<IMessageAuditor> auditors)
		{
			if (channel == null)
				throw new ArgumentNullException("channel");

			if (auditors == null)
				throw new ArgumentNullException("auditors");

			if (auditors.Count == 0)
				throw new ArgumentException("At least one auditor must be provided.", "auditors");

			this.channel = channel;
			this.auditors = auditors;
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

			foreach (var auditor in this.auditors)
			{
				Log.Verbose("Disposing auditor of type '{0}'.", auditor.GetType());
				auditor.TryDispose();
			}

			Log.Verbose("Disposing the underlying channel.");
			this.channel.TryDispose();
		}

		private static readonly ILog Log = LogFactory.Build(typeof(AuditChannel));
		private readonly IMessagingChannel channel;
		private readonly ICollection<IMessageAuditor> auditors;
		private IDeliveryContext currentContext;
		private bool disposed;
	}
}