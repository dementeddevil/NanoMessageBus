namespace NanoMessageBus
{
	using System;

	public class DefaultHandlerContext : IHandlerContext
	{
		public virtual IDeliveryContext Delivery
		{
			get { return this.delivery; }
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

		protected virtual void ThrowWhenDisposed()
		{
			if (this.disposed)
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

		private readonly IDeliveryContext delivery;
		private readonly IDispatchTable dispatchTable;
		private bool continueHandling;
		private bool disposed;
	}
}