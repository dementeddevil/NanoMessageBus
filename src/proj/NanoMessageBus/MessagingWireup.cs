namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Logging;

	/// <summary>
	/// Performs the primary wireup to create an active instance of the host.
	/// </summary>
	/// <remarks>
	/// This class is designed to be used during wireup and then thrown away.
	/// </remarks>
	public class MessagingWireup
	{
		public virtual MessagingWireup AddConnector(IChannelConnector connector)
		{
			Log.Debug("Adding channel connector of type '{0}'.", connector.GetType());

			connector = new DependencyResolverConnector(connector);
			this.connectors.Add(connector);
			return this;
		}
		public virtual MessagingWireup WithHandlerContext(Action<IDeliveryContext> delivery)
		{
			Log.Info("Alternate delivery callback provided.");

			this.receive = delivery;
			return this;
		}
		public virtual MessagingWireup WithDispatchTable(IDispatchTable table)
		{
			Log.Debug("Using dispatch table of type '{0}'.", table.GetType());

			this.dispatchTable = table;
			return this;
		}
		protected virtual void DefaultReceive(IDeliveryContext delivery)
		{
			Log.Verbose("Channel message received, routing message to configured handlers.");
			using (var context = new DefaultHandlerContext(delivery, this.dispatchTable))
				this.routingTable.Route(context, delivery.CurrentMessage);

			delivery.CurrentTransaction.Commit();
		}

		public virtual IMessagingHost Start()
		{
			Log.Info("Starting host in dispatch-only mode.");
			return this.StartHost();
		}
		public virtual IMessagingHost StartWithReceive(IRoutingTable table, Func<IHandlerContext, IMessageHandler<ChannelMessage>> handler = null)
		{
			Log.Info("Starting host in full-duplex mode.");

			var host = this.StartHost();
			this.routingTable = table;
			table.Add(handler ?? (context => new DefaultChannelMessageHandler(context, table)));
			host.BeginReceive(this.receive ?? this.DefaultReceive);
			return host;
		}

		protected virtual IMessagingHost StartHost()
		{
			var host = new DefaultMessagingHost(this.connectors.ToArray(), new DefaultChannelGroupFactory().Build);
			host.Initialize();
			return host;
		}

		private static readonly ILog Log = LogFactory.Builder(typeof(MessagingWireup));
		private readonly ICollection<IChannelConnector> connectors = new LinkedList<IChannelConnector>();
		private Action<IDeliveryContext> receive;
		private IRoutingTable routingTable;
		private IDispatchTable dispatchTable;
	}
}