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
			return new PooledDispatchChannel(this, channel, this.stateIndex);

			// TODO: how to safely clear the active channels when the connection becomes unavailable?
			// without having each and every channel throw and call back instructing the connector to
			// clear the active channel collection and restart?

			////CancellationTokenSource.cs
			//////m_state uses the pattern "volatile int32 reads, with cmpxch writes" which is safe for updates and cannot suffer torn reads. 
			////private volatile int m_state;

			////if (Interlocked.CompareExchange(ref m_state, NOTIFYING, NOT_CANCELED) == NOT_CANCELED) 
			////Interlocked.Exchange(ref m_state, NOTIFYINGCOMPLETE); 

			throw new System.NotImplementedException();
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