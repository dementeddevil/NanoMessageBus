namespace NanoMessageBus
{
	using System;
	using Logging;

	public class DefaultHandlerContext : IHandlerContext
	{
		public virtual ChannelMessage CurrentMessage
		{
			get { return this.delivery.CurrentMessage; }
		}
		public virtual IChannelTransaction CurrentTransaction
		{
			get { return this.delivery.CurrentTransaction; }
		}
		public virtual IChannelGroupConfiguration CurrentConfiguration
		{
			get { return this.delivery.CurrentConfiguration; }
		}
		public virtual IDependencyResolver CurrentResolver
		{
			get { return this.delivery.CurrentResolver; }
		}
		public virtual bool ContinueHandling
		{
			get { return this.continueHandling; }
		}
		public virtual void DropMessage()
		{
			this.ThrowWhenDisposed();
			this.continueHandling = false;
		}
		public virtual void DeferMessage()
		{
			this.DropMessage();

			this.delivery.Send(new ChannelEnvelope(
				this.delivery.CurrentMessage, new[] { ChannelEnvelope.LoopbackAddress }));
		}
		public virtual IDispatchContext PrepareDispatch(object message = null)
		{
			this.ThrowWhenDisposed();

			var context = new DefaultDispatchContext(this.delivery, this.dispatchTable);
			return message == null ? context : context.WithMessage(message);
		}

		public virtual void Send(ChannelEnvelope message)
		{
			// TODO: IDeliveryContext will be refactored to remove this method
			this.delivery.Send(message);
		}

		protected virtual void ThrowWhenDisposed()
		{
			if (!this.disposed)
				return;

			Log.Warn("The handler context has already been disposed.");
			throw new ObjectDisposedException(typeof(DefaultHandlerContext).Name);
		}

		public DefaultHandlerContext(IDeliveryContext delivery, IDispatchTable dispatchTable)
		{
			if (delivery == null)
				throw new ArgumentNullException("delivery");

			if (dispatchTable == null)
				throw new ArgumentNullException("dispatchTable");

			this.delivery = delivery;
			this.dispatchTable = dispatchTable;
			this.continueHandling = true;
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
			this.disposed = true;
			this.continueHandling = false;
		}

		private static readonly ILog Log = LogFactory.Build(typeof(DefaultHandlerContext));
		private readonly IDeliveryContext delivery;
		private readonly IDispatchTable dispatchTable;
		private bool continueHandling;
		private bool disposed;
	}
}