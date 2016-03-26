namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections.Generic;
	using Logging;

	public class DependencyResolverConnector : IChannelConnector
	{
		public virtual ConnectionState CurrentState => _connector.CurrentState;

	    public virtual IEnumerable<IChannelGroupConfiguration> ChannelGroups => _connector.ChannelGroups;

	    public virtual IMessagingChannel Connect(string channelGroup)
		{
			var channel = _connector.Connect(channelGroup);
			var resolver = channel.CurrentConfiguration.DependencyResolver;
			if (resolver == null)
			{
				Log.Verbose("No resolver configured, returning actual, non-decorated channel.");
				return channel;
			}

			try
			{
				Log.Verbose("Decorating channel inside a DependencyResolverChannel.");
				return new DependencyResolverChannel(channel, resolver.CreateNestedResolver());
			}
			catch
			{
				channel.TryDispose();
				throw;
			}
		}

		public DependencyResolverConnector(IChannelConnector connector)
		{
			if (connector == null)
			{
			    throw new ArgumentNullException(nameof(connector));
			}

		    _connector = connector;
		}
		~DependencyResolverConnector()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
			    _connector.TryDispose();
			}
		}

		private static readonly ILog Log = LogFactory.Build(typeof(DependencyResolverConnector));
		private readonly IChannelConnector _connector;
	}
}