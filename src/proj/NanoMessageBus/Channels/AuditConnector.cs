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
			get { return this.inner.CurrentState; }
		}
		public virtual IEnumerable<IChannelGroupConfiguration> ChannelGroups
		{
			get { return this.inner.ChannelGroups; }
		}
		public virtual IMessagingChannel Connect(string channelGroup)
		{
			var channel = this.inner.Connect(channelGroup);
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

			return this.factory(channel).Where(x => x != null).ToArray();
		}

		public AuditConnector(IChannelConnector inner, Func<IMessagingChannel, IEnumerable<IAuditListener>> factory)
		{
			if (inner == null)
				throw new ArgumentNullException("inner");

			if (factory == null)
				throw new ArgumentNullException("factory");

			this.inner = inner;
			this.factory = factory;
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
				this.inner.Dispose();
		}

		private static readonly ILog Log = LogFactory.Build(typeof(AuditConnector)); // TODO
		private readonly IChannelConnector inner;
		private readonly Func<IMessagingChannel, IEnumerable<IAuditListener>> factory;
		private bool emptyFactory;
	}
}