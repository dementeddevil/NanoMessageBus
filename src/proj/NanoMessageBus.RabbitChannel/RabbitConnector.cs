namespace NanoMessageBus.RabbitChannel
{
	using System;
	using System.Collections.Generic;
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

		public RabbitConnector(ConnectionFactory factory)
		{
			this.factory = factory;
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

		private readonly ConnectionFactory factory;
		private IConnection connection;
	}
}