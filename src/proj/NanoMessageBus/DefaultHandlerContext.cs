namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Logging;

	public class DefaultHandlerContext : IHandlerContext
	{
		public virtual bool Active => _delivery.Active;
	    public virtual ChannelMessage CurrentMessage => _delivery.CurrentMessage;
	    public virtual IChannelTransaction CurrentTransaction => _delivery.CurrentTransaction;

	    public virtual IChannelGroupConfiguration CurrentConfiguration => _delivery.CurrentConfiguration;
	    public virtual IDependencyResolver CurrentResolver => _delivery.CurrentResolver;

	    public virtual IDispatchContext PrepareDispatch(object message = null, IMessagingChannel channel = null)
		{
			ThrowWhenDisposed();
			return _delivery.PrepareDispatch(message, channel);
		}

		public virtual bool ContinueHandling => _continueHandling && Active;

	    public virtual void DropMessage()
		{
			ThrowWhenDisposed();
			_continueHandling = false;
		}
		public virtual void DeferMessage()
		{
			if (_deferred)
			{
			    return;
			}

		    _deferred = true;
			DropMessage();

			_delivery.PrepareDispatch()
				.WithMessage(_delivery.CurrentMessage)
				.WithRecipient(ChannelEnvelope.LoopbackAddress)
				.Send();
		}
		public virtual void ForwardMessage(IEnumerable<Uri> recipients)
		{
			ThrowWhenDisposed();

			if (recipients == null)
			{
			    throw new ArgumentNullException(nameof(recipients));
			}

		    var parsed = recipients.Where(x => x != null).ToArray();
			if (parsed.Length == 0)
			{
			    throw new ArgumentException("No recipients specified.", nameof(recipients));
			}

		    var dispatch = _delivery.PrepareDispatch(_delivery.CurrentMessage);
			foreach (var recipient in parsed)
			{
			    dispatch = dispatch.WithRecipient(recipient);
			}

		    dispatch.Send();
		}

		protected virtual void ThrowWhenDisposed()
		{
			if (!_disposed)
			{
			    return;
			}

		    Log.Warn("The handler context has already been disposed.");
			throw new ObjectDisposedException(typeof(DefaultHandlerContext).Name);
		}

		public DefaultHandlerContext(IDeliveryContext delivery)
		{
			if (delivery == null)
			{
			    throw new ArgumentNullException(nameof(delivery));
			}

		    _delivery = delivery;
			_continueHandling = true;
		}
		~DefaultHandlerContext()
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
			_disposed = true;
			_continueHandling = false;
		}

		private static readonly ILog Log = LogFactory.Build(typeof(DefaultHandlerContext));
		private readonly IDeliveryContext _delivery;
		private bool _continueHandling;
		private bool _disposed;
		private bool _deferred;
	}
}