namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
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
			this.ThrowWhenDisposed();

			ConcurrentBag<IMessagingChannel> items;
			if (!this.available.TryGetValue(channelGroup, out items))
				return this.connector.Connect(channelGroup);

			IMessagingChannel channel;
			if (!items.TryTake(out channel))
				channel = this.connector.Connect(channelGroup);

			this.open.Add(channel);
			return new PooledDispatchChannel(this, channel, this.currentToken);
		}

		public virtual void Release(IMessagingChannel channel, int token)
		{
			if (channel == null)
				throw new ArgumentNullException("channel");

			if (this.open.Remove(channel) && this.currentToken == token && this.currentToken >= 0)
				this.available[channel.CurrentConfiguration.GroupName].Add(channel);
			else
				throw new InvalidOperationException("Cannot release a channel that didn't originate with this connector.");
		}
		public virtual void Teardown(IMessagingChannel channel, int token)
		{
			if (channel == null)
				throw new ArgumentNullException("channel");

			if (!this.open.Contains(channel))
				throw new InvalidOperationException("Cannot tear down a channel that didn't originate with this connector.");

			if (this.FirstOneThrough(token, token + 1))
				this.ClearAvailableChannels();

			channel.Dispose();
		}
		private bool FirstOneThrough(int token, int assignment)
		{
#pragma warning disable 420
			// currentToken uses the pattern "volatile int32 reads, with cmpxch writes"
			// which is safe for updates and cannot suffer torn reads. 
			return Interlocked.CompareExchange(ref this.currentToken, assignment, token) == token;
#pragma warning restore 420
		}
		protected virtual void ClearAvailableChannels()
		{
			IMessagingChannel disconnected;
			foreach (var collection in this.available.Values)
				while (collection.TryTake(out disconnected))
					disconnected.Dispose();
		}

		protected virtual void ThrowWhenDisposed()
		{
			if (this.currentToken >= 0)
				return;

			Log.Warn("The channel has been disposed.");
			throw new ObjectDisposedException(typeof(PooledDispatchConnector).Name);
		}

		public PooledDispatchConnector(IChannelConnector connector) : this()
		{
			if (connector == null)
				throw new ArgumentNullException("connector");

			foreach (var config in connector.ChannelGroups.Where(x => x.DispatchOnly && x.Synchronous))
				this.available[config.GroupName] = new ConcurrentBag<IMessagingChannel>();

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

			if (this.currentToken < 0 || !this.FirstOneThrough(this.currentToken, Disposed))
				return; // already disposed

			this.ClearAvailableChannels();
			this.connector.Dispose();
		}

		private static readonly ILog Log = LogFactory.Build(typeof(PooledDispatchConnector)); // TODO
		private readonly ICollection<IMessagingChannel> open = new HashSet<IMessagingChannel>();
		private readonly IDictionary<string, ConcurrentBag<IMessagingChannel>> available =
			new Dictionary<string, ConcurrentBag<IMessagingChannel>>();
		private readonly IChannelConnector connector;
		private volatile int currentToken;
		private const int Disposed = int.MinValue;
	}
}