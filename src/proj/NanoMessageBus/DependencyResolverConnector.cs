namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;

	public class DependencyResolverConnector : IChannelConnector
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
			var resolver = channel.CurrentConfiguration.DependencyResolver;

			try
			{
				return new DependencyResolverChannel(channel, resolver.CreateNestedResolver());
			}
			catch
			{
				channel.Dispose();
				throw;
			}
		}

		public DependencyResolverConnector(IChannelConnector connector)
		{
			if (connector == null)
				throw new ArgumentNullException("connector");

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
			if (disposing)
				this.connector.Dispose();
		}

		private readonly IChannelConnector connector;
	}
}