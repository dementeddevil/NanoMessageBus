namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Logging;

	public class AuditConnector : IChannelConnector
	{
		public virtual ConnectionState CurrentState
		{
			get { return this.connector.CurrentState; }
		}
		public virtual IEnumerable<IChannelGroupConfiguration> ChannelGroups
		{
			get { return this.connector.ChannelGroups; }
		}
		public virtual IMessagingChannel Connect(string channelGroup)
		{
			var channel = this.connector.Connect(channelGroup);
			var listeners = this.ResolveListeners(channel);

			if (listeners.Count > 0)
				return new AuditChannel(channel, listeners);

			this.emptyFactory = true;
			return channel;
		}
		protected virtual ICollection<IAuditListener> ResolveListeners(IMessagingChannel channel)
		{
			if (this.emptyFactory)
				return new IAuditListener[0];

			return this.listenerFactory(channel).Where(x => x != null).ToArray();
		}

		public AuditConnector(IChannelConnector connector, Func<IMessagingChannel, IEnumerable<IAuditListener>> listenerFactory)
		{
			if (connector == null)
				throw new ArgumentNullException("connector");

			if (listenerFactory == null)
				throw new ArgumentNullException("listenerFactory");

			this.connector = connector;
			this.listenerFactory = listenerFactory;
		}
		~AuditConnector()
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
			if (disposing)
				this.connector.Dispose();
		}

		private static readonly ILog Log = LogFactory.Build(typeof(AuditConnector)); // TODO
		private readonly IChannelConnector connector;
		private readonly Func<IMessagingChannel, IEnumerable<IAuditListener>> listenerFactory;
		private bool emptyFactory;
	}
}