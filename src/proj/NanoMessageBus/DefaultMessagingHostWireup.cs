namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Performs the primary wireup to create an active instance of the host.
	/// </summary>
	/// <remarks>
	/// This class is designed to be used during wireup and then thrown away.
	/// </remarks>
	public class DefaultMessagingHostWireup
	{
		public virtual DefaultMessagingHostWireup AddConnector(IChannelConnector connector)
		{
			connector = new DependencyResolverConnector(connector);
			this.connectors.Add(connector);
			return this;
		}
		public virtual DefaultMessagingHostWireup WithHandlerContext(Action<IDeliveryContext> delivery)
		{
			this.receive = delivery;
			return this;
		}
		public virtual DefaultMessagingHostWireup WithDispatchTable(IDispatchTable table)
		{
			this.dispatchTable = table;
			return this;
		}
		protected virtual void DefaultReceive(IDeliveryContext delivery)
		{
			using (var context = new DefaultHandlerContext(delivery, this.dispatchTable))
				this.routingTable.Route(context, delivery.CurrentMessage);

			delivery.CurrentTransaction.Commit();
		}

		public virtual IMessagingHost Start()
		{
			var host = new DefaultMessagingHost(this.connectors.ToArray(), new DefaultChannelGroupFactory().Build);
			host.Initialize();
			return host;
		}
		public virtual IMessagingHost StartWithReceive(IRoutingTable table)
		{
			var host = this.Start();
			this.routingTable = table;
			host.BeginReceive(this.receive ?? this.DefaultReceive);
			return host;
		}

		private readonly ICollection<IChannelConnector> connectors = new LinkedList<IChannelConnector>();
		private Action<IDeliveryContext> receive;
		private IRoutingTable routingTable;
		private IDispatchTable dispatchTable;
	}
}