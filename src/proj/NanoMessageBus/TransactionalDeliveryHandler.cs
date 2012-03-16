namespace NanoMessageBus
{
	using System;
	using Logging;

	public class TransactionalDeliveryHandler : IDeliveryHandler
	{
		public void Handle(IDeliveryContext delivery)
		{
			if (delivery == null)
				throw new ArgumentNullException("delivery");

			Log.Debug("Channel message delivery received, routing to inner delivery handler.");
			this.inner.Handle(delivery);

			Log.Debug("Committing transaction associated with delivery.");
			delivery.CurrentTransaction.Commit();
		}

		public TransactionalDeliveryHandler(IDeliveryHandler inner)
		{
			if (inner == null)
				throw new ArgumentNullException("inner");

			this.inner = inner;
		}

		private static readonly ILog Log = LogFactory.Build(typeof(TransactionalDeliveryHandler));
		private readonly IDeliveryHandler inner;
	}
}