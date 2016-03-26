namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections.Generic;
	using Logging;

	public class AuditChannel : IMessagingChannel
	{
		public virtual bool Active
		{
			get { return this.CurrentContext.Active; }
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
			get { return this._currentContext ?? this._channel; }
		}

		public virtual IDispatchContext PrepareDispatch(object message = null, IMessagingChannel actual = null)
		{
			Log.Debug("Preparing a dispatch");
			return this.CurrentContext.PrepareDispatch(message, actual ?? this);
		}
		public virtual void Send(ChannelEnvelope envelope)
		{
			if (envelope == null)
				throw new ArgumentNullException(nameof(envelope));

			this.ThrowWhenDisposed();

			this.CurrentTransaction.Register(() => this.AuditSend(envelope));

			Log.Verbose("Sending envelope through the underlying channel.", envelope.MessageId());
			this._channel.Send(envelope);
		}
		private void AuditSend(ChannelEnvelope envelope)
		{
			var messageId = envelope.MessageId();
			foreach (var auditor in this._auditors)
			{
				Log.Debug("Providing envelope '{0}' for inspection to auditor of type '{1}'.", messageId, auditor.GetType());
				auditor.AuditSend(envelope, this);
			}
		}

		public virtual void BeginShutdown()
		{
			this._channel.BeginShutdown();
		}
		public virtual void Receive(Action<IDeliveryContext> callback)
		{
			this._channel.Receive(context => this.Receive(context, callback));
		}
		protected virtual void Receive(IDeliveryContext context, Action<IDeliveryContext> callback)
		{
			try
			{
				this.AuditReceive(context);

				Log.Verbose("Routing delivery to configured callback.");
				this._currentContext = context;
				callback(this);
			}
			finally
			{
				this._currentContext = null;
			}
		}
		private void AuditReceive(IDeliveryContext context)
		{
			var messageId = context.CurrentMessage.MessageId;

			foreach (var auditor in this._auditors)
			{
				Log.Debug("Routing delivery for message '{0}' for inspection to auditor of type '{1}'.", messageId, auditor.GetType());
				auditor.AuditReceive(context);
			}
		}

		protected virtual void ThrowWhenDisposed()
		{
			if (!this._disposed)
				return;

			Log.Warn("The channel has been disposed.");
			throw new ObjectDisposedException(typeof(AuditChannel).Name);
		}

		public AuditChannel(IMessagingChannel channel, ICollection<IMessageAuditor> auditors)
		{
			if (channel == null)
				throw new ArgumentNullException(nameof(channel));

			if (auditors == null)
				throw new ArgumentNullException(nameof(auditors));

			if (auditors.Count == 0)
				throw new ArgumentException("At least one auditor must be provided.", nameof(auditors));

			this._channel = channel;
			this._auditors = new List<IMessageAuditor>(auditors);
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
			if (!disposing || this._disposed)
				return;

			this._disposed = true;

			foreach (var auditor in this._auditors)
			{
				Log.Verbose("Disposing auditor of type '{0}'.", auditor.GetType());
				auditor.TryDispose();
			}

			this._auditors.Clear();

			Log.Verbose("Disposing the underlying channel.");
			this._channel.TryDispose();
		}

		private static readonly ILog Log = LogFactory.Build(typeof(AuditChannel));
		private readonly IMessagingChannel _channel;
		private readonly ICollection<IMessageAuditor> _auditors;
		private IDeliveryContext _currentContext;
		private bool _disposed;
	}
}