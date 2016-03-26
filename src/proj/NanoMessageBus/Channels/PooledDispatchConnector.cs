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
			get { return this._connector.CurrentState; }
		}
		public virtual IEnumerable<IChannelGroupConfiguration> ChannelGroups
		{
			get { return this._connector.ChannelGroups; }
		}
		public virtual IMessagingChannel Connect(string channelGroup)
		{
			this.ThrowWhenDisposed();

			ConcurrentBag<IMessagingChannel> items;
			if (!this._available.TryGetValue(channelGroup, out items))
			{
				Log.Debug("Channel group '{0}' is not pooled, resolving through underlying connector.", channelGroup);
				return this._connector.Connect(channelGroup);
			}

			while (true)
			{
				var channel = this.TryConnect(channelGroup, items); // throws if underlying connection isn't available
				if (channel.Active)
					return channel;

				channel.Dispose();
			}
		}
		private IMessagingChannel TryConnect(string channelGroup, IProducerConsumerCollection<IMessagingChannel> items)
		{
			IMessagingChannel channel;
			if (!items.TryTake(out channel))
			{
				Log.Debug("No available channel in the pool for '{0}', establishing a new channel.", channelGroup);
				channel = this._connector.Connect(channelGroup);
			}

			Log.Verbose("Resolving channel for '{0}' from the pool of available channels.", channelGroup);
			this._open.TryAdd(channel, true);
			return new PooledDispatchChannel(this, channel, this._currentToken);
		}

		public virtual void Release(IMessagingChannel channel, int token)
		{
			if (channel == null)
				throw new ArgumentNullException(nameof(channel));

			Log.Verbose("Trying to release the channel back to the pool for reuse.");

			bool value;
			if (!this._open.TryRemove(channel, out value))
				throw new InvalidOperationException("Cannot release a channel that didn't originate with this connector.");

			if (this._currentToken >= 0 && this._currentToken == token && channel.Active)
				this._available[channel.CurrentConfiguration.GroupName].Add(channel);
			else
				channel.TryDispose();
		}
		public virtual void Teardown(IMessagingChannel channel, int token)
		{
			if (channel == null)
				throw new ArgumentNullException(nameof(channel));

			if (!this._open.ContainsKey(channel))
				throw new InvalidOperationException("Cannot tear down a channel that didn't originate with this connector.");

			if (this.FirstOneThrough(token, token + 1))
				this.ClearAvailableChannels();

			Log.Verbose("Tearing down channel.");
			channel.TryDispose();
		}
		private bool FirstOneThrough(int token, int assignment)
		{
#pragma warning disable 420
			// currentToken uses the pattern "volatile int32 reads, with cmpxch writes"
			// which is safe for updates and cannot suffer torn reads. 
			return Interlocked.CompareExchange(ref this._currentToken, assignment, token) == token;
#pragma warning restore 420
		}
		protected virtual void ClearAvailableChannels()
		{
			Log.Debug("Clearing available channels; underlying connection no longer available.");

			var count = 0;
			foreach (var collection in this._available.Values)
			{
				IMessagingChannel disconnected;
				while (collection.TryTake(out disconnected))
				{
					count++;
					disconnected.TryDispose();
				}
			}

			Log.Debug("{0} pooled channels disposed across {1} channel groups.", count, this._available.Values.Count);
		}

		protected virtual void ThrowWhenDisposed()
		{
			if (this._currentToken >= 0)
				return;

			Log.Warn("The channel has been disposed.");
			throw new ObjectDisposedException(typeof(PooledDispatchConnector).Name);
		}

		public PooledDispatchConnector(IChannelConnector connector) : this()
		{
			if (connector == null)
				throw new ArgumentNullException(nameof(connector));

			foreach (var config in connector.ChannelGroups.Where(x => x.DispatchOnly && x.Synchronous))
			{
				Log.Info("Channels for group '{0}' will be pooled.", config.GroupName);
				this._available[config.GroupName] = new ConcurrentBag<IMessagingChannel>();
			}

			Log.Debug("{0} pooled channel groups configured.", this._available.Values.Count);
			this._connector = connector;
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

			if (this._currentToken < 0 || !this.FirstOneThrough(this._currentToken, Disposed))
			{
				Log.Debug("Connector has already been disposed.");
				return; // already disposed
			}

			this.ClearAvailableChannels();

			Log.Debug("Disposing the underlying connection.");
			this._connector.TryDispose();
		}

		private static readonly ILog Log = LogFactory.Build(typeof(PooledDispatchConnector));
		private readonly ConcurrentDictionary<IMessagingChannel, bool> _open =
			new ConcurrentDictionary<IMessagingChannel, bool>();
		private readonly IDictionary<string, ConcurrentBag<IMessagingChannel>> _available =
			new Dictionary<string, ConcurrentBag<IMessagingChannel>>();
		private readonly IChannelConnector _connector;
		private volatile int _currentToken;
		private const int Disposed = int.MinValue;
	}
}