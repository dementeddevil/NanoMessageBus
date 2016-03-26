using System;
using System.Threading.Tasks;
using NanoMessageBus.Logging;

namespace NanoMessageBus
{
	public class DefaultDeliveryHandler : IDeliveryHandler
	{
		private static readonly ILog Log = LogFactory.Build(typeof(DefaultDeliveryHandler));
		private readonly IRoutingTable _routingTable;

		public DefaultDeliveryHandler(IRoutingTable routingTable)
		{
		    if (routingTable == null)
		    {
		        throw new ArgumentNullException(nameof(routingTable));
		    }

			_routingTable = routingTable;
		}

        public virtual async Task HandleAsync(IDeliveryContext delivery)
		{
			Log.Debug("Channel message received, routing message to configured handlers.");

		    using (var context = new DefaultHandlerContext(delivery))
		    {
		        await _routingTable.Route(context, delivery.CurrentMessage).ConfigureAwait(false);
		    }

			Log.Verbose("Channel message payload successfully delivered to all configured recipients.");
		}
	}
}