namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections.Generic;
	using Logging;

	public class DependencyResolverConnector : IChannelConnector
	{
		public virtual ConnectionState CurrentState
		{
			get { return this._connector.CurrentState; }
		}
		public virtual IEnumerable<IChannelGroupConfiguration> ChannelGroups
		{
			get { return this._connector.ChannelGroups; }
		}
		public virtual IMessagingChannel Connect(string channelGroup)
		{
			var channel = this._connector.Connect(channelGroup);
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
				throw new ArgumentNullException(nameof(connector));

			this._connector = connector;
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
				this._connector.TryDispose();
		}

		private static readonly ILog Log = LogFactory.Build(typeof(DependencyResolverConnector));
		private readonly IChannelConnector _connector;
	}
}