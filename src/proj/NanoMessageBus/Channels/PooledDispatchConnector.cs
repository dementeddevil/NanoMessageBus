namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using Logging;

	public class PooledDispatchConnector : IChannelConnector
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
			var config = channel.CurrentConfiguration;
			if (config.DispatchOnly && config.Synchronous)
				return new PooledDispatchChannel(this, channel, this.stateIndex);

			return channel;
		}

		public virtual void Release(IMessagingChannel channel, int state)
		{
			throw new NotImplementedException();
		}

		public PooledDispatchConnector(IChannelConnector connector) : this()
		{
			if (connector == null)
				throw new ArgumentNullException("connector");

			this.connector = connector;
		}
		protected PooledDispatchConnector()
		{
		}
		~PooledDispatchConnector()
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

			// TODO: clear the channels collection and reset the state index to zero?
			this.connector.Dispose();
		}

		private static readonly ILog Log = LogFactory.Build(typeof(PooledDispatchConnector));
		private readonly ConcurrentBag<IMessagingChannel> channels = new ConcurrentBag<IMessagingChannel>();
		private readonly IChannelConnector connector;
		private int stateIndex;
	}
}