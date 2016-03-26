namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Web;
	using Channels;
	using Logging;

	/// <summary>
	/// Performs the primary wireup to create an active instance of the host.
	/// </summary>
	/// <remarks>
	/// This class is designed to be used during the wireup process and then thrown away.
	/// </remarks>
	public class MessagingWireup
	{
		public virtual MessagingWireup AddConnector(IChannelConnector channelConnector)
		{
			if (channelConnector == null)
				throw new ArgumentNullException(nameof(channelConnector));

			Log.Debug("Adding channel connector of type '{0}'.", channelConnector.GetType());

			if (channelConnector.GetType() != typeof(PooledDispatchConnector))
				channelConnector = new PooledDispatchConnector(channelConnector);

			if (channelConnector.GetType() != typeof(DependencyResolverConnector))
				channelConnector = new DependencyResolverConnector(channelConnector);

			this._connectors.Add(channelConnector);
			return this;
		}
		public virtual MessagingWireup AddConnectors(IEnumerable<IChannelConnector> channelConnectors)
		{
			foreach (var connector in channelConnectors)
				this.AddConnector(connector);

			return this;
		}

		public virtual MessagingWireup WithAuditing()
		{
			return this.WithAuditing(x => new IMessageAuditor[0]); // append existing auditors
		}
		public virtual MessagingWireup WithAuditing(Func<IMessagingChannel, IEnumerable<IMessageAuditor>> auditors)
		{
			if (auditors == null)
				throw new ArgumentNullException();

			this._auditorFactory = x => auditors(x).Concat(this.AppendAuditors());
			return this;
		}
		public virtual MessagingWireup WithAuditing<TResolver>(Func<TResolver, IEnumerable<IMessageAuditor>> auditors) where TResolver : class
		{
			return this.WithAuditing(channel =>
			{
				var resolver = channel.CurrentResolver;
				return resolver == null ? new IMessageAuditor[0] : auditors(channel.CurrentResolver.As<TResolver>());
			});
		}
		protected virtual IEnumerable<IMessageAuditor> AppendAuditors()
		{
			yield return new PointOfOriginAuditor();
			yield return new CloudAuditor();

			var context = HttpContext.Current;
			if (context == null)
				yield return new HttpRequestAuditor(null);
			else
				yield return new HttpRequestAuditor(new HttpContextWrapper(context));
		}

		public virtual MessagingWireup WithDeliveryHandler(Func<IDeliveryHandler, IDeliveryHandler> callback)
		{
			Log.Info("Alternate delivery handler provided.");

			this._handlerCallback = callback;
			return this;
		}
		public virtual MessagingWireup WithTransactionScope()
		{
			this._transactionScope = true;
			return this;
		}

		public virtual IMessagingHost Start()
		{
			Log.Info("Starting host in dispatch-only mode; duplex mode can be started later, if configured.");
			return this.StartHost();
		}
		public virtual IMessagingHost StartWithReceive(
            IRoutingTable table,
            Func<IHandlerContext, IMessageHandler<ChannelMessage>> handler = null)
		{
			Log.Info("Starting host in full-duplex mode.");

		    if (handler != null)
		    {
                table.Add(handler, 0, typeof(DefaultChannelMessageHandler));
		    }
		    else
		    {
		        table.Add(context => new DefaultChannelMessageHandler(context, table), 0, typeof(DefaultChannelMessageHandler));
		    }

			var host = this.StartHost();
			host.BeginReceive(this.BuildDeliveryChain(table).HandleAsync);
			return host;
		}

		protected virtual IDeliveryHandler BuildDeliveryChain(IRoutingTable table)
		{
			IDeliveryHandler handler = new DefaultDeliveryHandler(table);

			if (this._handlerCallback != null)
				handler = this._handlerCallback(handler) ?? handler;

			if (this._transactionScope)
				handler = new TransactionScopeDeliveryHandler(handler);

			return new TransactionalDeliveryHandler(handler);
		}

		protected virtual IMessagingHost StartHost()
		{
			this.AuditConnection();

			var host = new DefaultMessagingHost(this._connectors, new DefaultChannelGroupFactory().Build);
			host.Initialize();
			return host;
		}
		protected virtual void AuditConnection()
		{
			if (this._auditorFactory == null)
				return;

			// AuditConnector -> DependencyResolverConnector -> PooledConnector -> RabbitConnector
			for (var i = 0; i < this._connectors.Count; i++)
				this._connectors[i] = new AuditConnector(this._connectors[i], this._auditorFactory);
		}

		private static readonly ILog Log = LogFactory.Build(typeof(MessagingWireup));
		private readonly IList<IChannelConnector> _connectors = new List<IChannelConnector>();
		private Func<IDeliveryHandler, IDeliveryHandler> _handlerCallback;
		private Func<IMessagingChannel, IEnumerable<IMessageAuditor>> _auditorFactory;
		private bool _transactionScope;
	}
}