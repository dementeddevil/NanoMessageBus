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

			try
			{
				return new DependencyResolverChannel(channel, this.resolver.CreateNestedResolver());
			}
			catch
			{
				channel.Dispose();
				throw;
			}
		}

		public DependencyResolverConnector(IChannelConnector connector, IDependencyResolver resolver)
		{
			if (connector == null)
				throw new ArgumentNullException("connector");

			if (resolver == null)
				throw new ArgumentNullException("resolver");

			this.connector = connector;
			this.resolver = resolver;
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
			if (!disposing)
				return;

			this.connector.Dispose();
			this.resolver.Dispose();
		}

		private readonly IChannelConnector connector;
		private readonly IDependencyResolver resolver;
	}
}