namespace NanoMessageBus.RabbitChannel
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RabbitMQ.Client;

	public class RabbitConnector : IChannelConnector
	{
		public virtual ConnectionState CurrentState
		{
			get { return 0; }
		}
		public virtual IEnumerable<IChannelGroupConfiguration> ChannelGroups
		{
			get { return new IChannelGroupConfiguration[0]; }
		}
		public virtual IMessagingChannel Connect(string channelGroup)
		{
			return null;
		}

		public RabbitConnector(ConnectionFactory factory, IEnumerable<IChannelGroupConfiguration> configuration)
			: this()
		{
			this.factory = factory;
			this.configuration = configuration
				.Where(x => x != null)
				.ToDictionary(x => x.GroupName, x => x);
		}
		protected RabbitConnector()
		{
		}
		~RabbitConnector()
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
		}

		private readonly IDictionary<string, IChannelGroupConfiguration> configuration;
		private readonly ConnectionFactory factory;
		private IConnection connection;
	}
}