namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections.Generic;
	using Logging;

	public class AuditConnector : IChannelConnector
	{
		public virtual ConnectionState CurrentState
		{
			get { throw new NotImplementedException(); }
		}
		public virtual IEnumerable<IChannelGroupConfiguration> ChannelGroups
		{
			get { throw new NotImplementedException(); }
		}
		public virtual IMessagingChannel Connect(string channelGroup)
		{
			// TODO: don't decorate the channel if there aren't any listeners
			// TODO: create various default audit listeners
			throw new NotImplementedException();
		}

		public AuditConnector(IChannelConnector inner, Func<IMessagingChannel, IEnumerable<IAuditListener>> factory)
		{
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
			throw new NotImplementedException();
		}

		private static readonly ILog Log = LogFactory.Build(typeof(AuditConnector));
		private readonly IChannelConnector inner;
		private readonly Func<IMessagingChannel, IEnumerable<IAuditListener>> factory;
	}
}