namespace NanoMessageBus
{
	using System;
	using Logging;

	public class DefaultDeliveryHandler : IDeliveryHandler
	{
		public void Handle(IDeliveryContext delivery)
		{
			if (delivery == null)
				throw new ArgumentNullException("delivery");

			Log.Verbose("Channel message received, routing message to configured handlers.");
			using (var context = new DefaultHandlerContext(delivery))
				this.routingTable.Route(context, delivery.CurrentMessage);

			Log.Verbose("Channel message receipt completed, committing current transaction.");
			delivery.CurrentTransaction.Commit();
		}

		public DefaultDeliveryHandler(IRoutingTable routingTable)
		{
			if (routingTable == null)
				throw new ArgumentNullException("routingTable");

			this.routingTable = routingTable;
		}

		private readonly IRoutingTable routingTable;
		private static readonly ILog Log = LogFactory.Build(typeof(DefaultDeliveryHandler));
	}
}