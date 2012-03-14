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
		public virtual MessagingWireup AddConnector(IChannelConnector channelConnector)
		{
			Log.Debug("Adding channel connector of type '{0}'.", channelConnector.GetType());

			channelConnector = new DependencyResolverConnector(channelConnector);
			this.connectors.Add(channelConnector);
			return this;
		}
		public virtual MessagingWireup AddConnectors(IEnumerable<IChannelConnector> channelConnectors)
		{
			foreach (var connector in channelConnectors)
				this.AddConnector(connector);

			return this;
		}
		public virtual MessagingWireup WithDeliveryCallback(Action<IDeliveryContext> callback)
		{
			Log.Info("Alternate delivery callback provided.");

			this.deliveryCallback = callback;
			return this;
		}
		public virtual MessagingWireup WrapDeliveryCallback(Action<IDeliveryContext, Action<IDeliveryContext>> callback)
		{
			Log.Info("Wrapping delivery callback provided.");

			this.wrappedCallback = callback;
			return this;
		}
		protected virtual void DefaultReceive(IDeliveryContext delivery)
		{
			Log.Verbose("Channel message received, routing message to configured handlers.");
			using (var context = new DefaultHandlerContext(delivery))
				this.routingTable.Route(context, delivery.CurrentMessage);

			Log.Verbose("Channel message receipt completed, committing current transaction.");
			delivery.CurrentTransaction.Commit();
		}

		public virtual IMessagingHost Start()
		{
			Log.Info("Starting host in dispatch-only mode; duplex mode can be started later, if configured.");
			return this.StartHost();
		}
		public virtual IMessagingHost StartWithReceive(IRoutingTable table, Func<IHandlerContext, IMessageHandler<ChannelMessage>> handler = null)
		{
			Log.Info("Starting host in full-duplex mode.");

			var host = this.StartHost();
			this.routingTable = table;
			table.Add(handler ?? (context => new DefaultChannelMessageHandler(context, table)));
			host.BeginReceive(this.StartReceive);
			return host;
		}
		private void StartReceive(IDeliveryContext context)
		{
			var callback = this.deliveryCallback ?? this.DefaultReceive;

			if (this.wrappedCallback == null)
				callback(context);
			else
				this.wrappedCallback(context, callback);
		}

		protected virtual IMessagingHost StartHost()
		{
			var host = new DefaultMessagingHost(this.connectors.ToArray(), new DefaultChannelGroupFactory().Build);
			host.Initialize();
			return host;
		}

		private static readonly ILog Log = LogFactory.Build(typeof(MessagingWireup));
		private readonly ICollection<IChannelConnector> connectors = new LinkedList<IChannelConnector>();
		private Action<IDeliveryContext> deliveryCallback;
		private Action<IDeliveryContext, Action<IDeliveryContext>> wrappedCallback;
		private IRoutingTable routingTable;
	}
}