namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;

	public class DependencyResolverConnector : IChannelConnector
	{
		public ConnectionState CurrentState
		{
			get { return 0; }
		}
		public IEnumerable<IChannelGroupConfiguration> ChannelGroups
		{
			get { return null; }
		}
		public IMessagingChannel Connect(string channelGroup)
		{
			return null;
		}

		public DependencyResolverConnector(IChannelConnector connector)
		{
			this.connector = connector;
		}
		~DependencyResolverConnector()
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
		}

		private readonly IChannelConnector connector;
	}
}