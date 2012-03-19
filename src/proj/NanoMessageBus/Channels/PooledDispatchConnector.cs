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
			get { throw new System.NotImplementedException(); }
		}
		public virtual IEnumerable<IChannelGroupConfiguration> ChannelGroups
		{
			get { throw new System.NotImplementedException(); }
		}
		public virtual IMessagingChannel Connect(string channelGroup)
		{
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

		public PooledDispatchConnector(IChannelConnector connector)
		{
			this.connector = connector;
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
			throw new System.NotImplementedException();
		}

		private static readonly ILog Log = LogFactory.Build(typeof(PooledDispatchConnector));
		private readonly ConcurrentBag<IMessagingChannel> channels;
		private readonly IChannelConnector connector;
	}
}