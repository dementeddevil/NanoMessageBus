namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class DefaultMessagingHostWireup
	{
		public virtual DefaultMessagingHostWireup AddConnector(Func<IChannelConnector> callback)
		{
			this.connectors.Add(new DependencyResolverConnector(callback()));
			return this;
		}
		public virtual DefaultMessagingHostWireup WithHandlerContext(Action<IDeliveryContext> delivery)
		{
			this.receive = delivery;
			return this;
		}
		public virtual DefaultMessagingHostWireup WithRoutingTable(IRoutingTable table)
		{
			this.routingTable = table;
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

		public virtual IMessagingHost Build()
		{
			var host = new DefaultMessagingHost(this.connectors.ToArray(), new DefaultChannelGroupFactory().Build);
			host.Initialize();
			host.BeginReceive(this.receive ?? this.DefaultReceive);
			return host;
		}

		private readonly ICollection<IChannelConnector> connectors = new LinkedList<IChannelConnector>();
		private Action<IDeliveryContext> receive;
		private IRoutingTable routingTable;
		private IDispatchTable dispatchTable;
	}
}