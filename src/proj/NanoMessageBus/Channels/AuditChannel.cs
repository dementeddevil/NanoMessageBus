using System;
using System.Collections.Generic;
using NanoMessageBus.Logging;
using System.Threading.Tasks;

namespace NanoMessageBus.Channels
{
    public class AuditChannel : IMessagingChannel
	{
		public virtual bool Active => CurrentContext.Active;

	    public virtual ChannelMessage CurrentMessage => CurrentContext.CurrentMessage;

	    public virtual IChannelTransaction CurrentTransaction => CurrentContext.CurrentTransaction;

	    public virtual IChannelGroupConfiguration CurrentConfiguration => CurrentContext.CurrentConfiguration;

	    public virtual IDependencyResolver CurrentResolver => CurrentContext.CurrentResolver;

	    protected virtual IDeliveryContext CurrentContext => _currentContext ?? _channel;

	    public virtual IDispatchContext PrepareDispatch(object message = null, IMessagingChannel actual = null)
		{
			Log.Debug("Preparing a dispatch");
			return CurrentContext.PrepareDispatch(message, actual ?? this);
		}

		public virtual Task SendAsync(ChannelEnvelope envelope)
		{
			if (envelope == null)
			{
			    throw new ArgumentNullException(nameof(envelope));
			}

		    ThrowWhenDisposed();

			CurrentTransaction.Register(() => AuditSend(envelope));

			Log.Verbose("Sending envelope through the underlying channel.", envelope.MessageId());
			return _channel.SendAsync(envelope);
		}

		private void AuditSend(ChannelEnvelope envelope)
		{
			var messageId = envelope.MessageId();
			foreach (var auditor in _auditors)
			{
				Log.Debug("Providing envelope '{0}' for inspection to auditor of type '{1}'.", messageId, auditor.GetType());
				auditor.AuditSend(envelope, this);
			}
		}

		public virtual Task ShutdownAsync()
		{
			return _channel.ShutdownAsync();
		}

		public virtual Task ReceiveAsync(Func<IDeliveryContext, Task> callback)
		{
			return _channel.ReceiveAsync(context => ReceiveAsync(context, callback));
		}

		protected virtual async Task ReceiveAsync(IDeliveryContext context, Func<IDeliveryContext, Task> callback)
		{
			try
			{
				AuditReceive(context);

				Log.Verbose("Routing delivery to configured callback.");
				_currentContext = context;
				await callback(this).ConfigureAwait(false);
			}
			finally
			{
				_currentContext = null;
			}
		}

		private void AuditReceive(IDeliveryContext context)
		{
			var messageId = context.CurrentMessage.MessageId;

			foreach (var auditor in _auditors)
			{
				Log.Debug("Routing delivery for message '{0}' for inspection to auditor of type '{1}'.", messageId, auditor.GetType());
				auditor.AuditReceive(context);
			}
		}

		protected virtual void ThrowWhenDisposed()
		{
			if (!_disposed)
			{
			    return;
			}

		    Log.Warn("The channel has been disposed.");
			throw new ObjectDisposedException(typeof(AuditChannel).Name);
		}

		public AuditChannel(IMessagingChannel channel, ICollection<IMessageAuditor> auditors)
		{
			if (channel == null)
			{
			    throw new ArgumentNullException(nameof(channel));
			}

		    if (auditors == null)
		    {
		        throw new ArgumentNullException(nameof(auditors));
		    }

		    if (auditors.Count == 0)
		    {
		        throw new ArgumentException("At least one auditor must be provided.", nameof(auditors));
		    }

		    _channel = channel;
			_auditors = new List<IMessageAuditor>(auditors);
		}
		~AuditChannel()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing || _disposed)
			{
			    return;
			}

		    _disposed = true;

			foreach (var auditor in _auditors)
			{
				Log.Verbose("Disposing auditor of type '{0}'.", auditor.GetType());
				auditor.TryDispose();
			}

			_auditors.Clear();

			Log.Verbose("Disposing the underlying channel.");
			_channel.TryDispose();
		}

		private static readonly ILog Log = LogFactory.Build(typeof(AuditChannel));
		private readonly IMessagingChannel _channel;
		private readonly ICollection<IMessageAuditor> _auditors;
		private IDeliveryContext _currentContext;
		private bool _disposed;
	}
}