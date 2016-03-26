namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Logging;

	public class DefaultHandlerContext : IHandlerContext
	{
		public virtual bool Active
		{
			get { return this._delivery.Active; }
		}
		public virtual ChannelMessage CurrentMessage
		{
			get { return this._delivery.CurrentMessage; }
		}
		public virtual IChannelTransaction CurrentTransaction
		{
			get { return this._delivery.CurrentTransaction; }
		}
		public virtual IChannelGroupConfiguration CurrentConfiguration
		{
			get { return this._delivery.CurrentConfiguration; }
		}
		public virtual IDependencyResolver CurrentResolver
		{
			get { return this._delivery.CurrentResolver; }
		}
		public virtual IDispatchContext PrepareDispatch(object message = null, IMessagingChannel channel = null)
		{
			this.ThrowWhenDisposed();
			return this._delivery.PrepareDispatch(message, channel);
		}

		public virtual bool ContinueHandling
		{
			get { return this._continueHandling && this.Active; }
		}
		public virtual void DropMessage()
		{
			this.ThrowWhenDisposed();
			this._continueHandling = false;
		}
		public virtual void DeferMessage()
		{
			if (this._deferred)
				return;

			this._deferred = true;
			this.DropMessage();

			this._delivery.PrepareDispatch()
				.WithMessage(this._delivery.CurrentMessage)
				.WithRecipient(ChannelEnvelope.LoopbackAddress)
				.Send();
		}
		public virtual void ForwardMessage(IEnumerable<Uri> recipients)
		{
			this.ThrowWhenDisposed();

			if (recipients == null)
				throw new ArgumentNullException(nameof(recipients));

			var parsed = recipients.Where(x => x != null).ToArray();
			if (parsed.Length == 0)
				throw new ArgumentException("No recipients specified.", nameof(recipients));

			var dispatch = this._delivery.PrepareDispatch(this._delivery.CurrentMessage);
			foreach (var recipient in parsed)
				dispatch = dispatch.WithRecipient(recipient);

			dispatch.Send();
		}

		protected virtual void ThrowWhenDisposed()
		{
			if (!this._disposed)
				return;

			Log.Warn("The handler context has already been disposed.");
			throw new ObjectDisposedException(typeof(DefaultHandlerContext).Name);
		}

		public DefaultHandlerContext(IDeliveryContext delivery)
		{
			if (delivery == null)
				throw new ArgumentNullException(nameof(delivery));

			this._delivery = delivery;
			this._continueHandling = true;
		}
		~DefaultHandlerContext()
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
			this._disposed = true;
			this._continueHandling = false;
		}

		private static readonly ILog Log = LogFactory.Build(typeof(DefaultHandlerContext));
		private readonly IDeliveryContext _delivery;
		private bool _continueHandling;
		private bool _disposed;
		private bool _deferred;
	}
}