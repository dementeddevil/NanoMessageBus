namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Linq;

	public class DefaultMessagingHost : IMessagingHost
	{
		public virtual void Initialize()
		{
			lock (this.sync)
			{
				this.ThrowWhenDisposed();

				if (!this.initialized)
					this.InitializeChannelGroups();

				this.initialized = true;
			}
		}
		protected virtual void InitializeChannelGroups()
		{
			foreach (var connector in this.connectors)
				foreach (var config in connector.ChannelGroups)
					this.AddChannelGroup(config.GroupName, this.factory(connector, config));

			if (this.inbound.Count == 0)
				throw new ConfigurationErrorsException("No channel groups have been configured.");
		}
		protected virtual void AddChannelGroup(string name, IChannelGroup group)
		{
			var collection = group.DispatchOnly ? this.outbound : this.inbound;
			collection[name] = group;
		}

		public virtual IOutboundChannel GetOutboundChannel(string channelGroup)
		{
			if (channelGroup == null)
				throw new ArgumentNullException("channelGroup");

			lock (this.sync)
			{
				this.ThrowWhenDisposed();
				this.ThrowWhenUninitialized();

				IChannelGroup group;
				if (this.outbound.TryGetValue(channelGroup, out group) && group.DispatchOnly)
					return group;

				throw new KeyNotFoundException("Could not find a dispatch-only channel group from the key provided.");
			}
		}

		public virtual void BeginReceive(Action<IDeliveryContext> callback)
		{
			if (callback == null)
				throw new ArgumentNullException("callback");

			lock (this.sync)
			{
				this.ThrowWhenDisposed();
				this.ThrowWhenUninitialized();
				this.ThrowWhenReceiving();
				this.receiving = true;

				foreach (var group in this.inbound.Values)
					group.BeginReceive(callback);
			}
		}

		protected virtual void ThrowWhenDisposed()
		{
			if (this.disposed)
				throw new ObjectDisposedException(typeof(DefaultMessagingHost).Name);
		}
		protected virtual void ThrowWhenUninitialized()
		{
			if (!this.initialized)
				throw new InvalidOperationException("The host has not been initialized.");
		}
		protected virtual void ThrowWhenReceiving()
		{
			if (this.receiving)
				throw new InvalidOperationException("A callback has already been provided.");
		}

		public DefaultMessagingHost(IEnumerable<IChannelConnector> connectors, ChannelGroupFactory factory)
		{
			if (connectors == null)
				throw new ArgumentNullException("connectors");

			if (factory == null)
				throw new ArgumentNullException("factory");

			this.connectors = connectors.Where(x => x != null).ToArray();
			if (this.connectors.Count == 0)
				throw new ArgumentException("No connectors provided.", "connectors");

			this.factory = factory;
		}
		~DefaultMessagingHost()
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

			lock (this.sync)
			{
				if (this.disposed)
					return;

				this.disposed = true;

				// TODO: dispose outbound group also
				foreach (var group in this.inbound.Values)
					group.Dispose(); // TODO: make sure this doesn't wrap around and result in a deadlock

				this.inbound.Clear();
			}
		}

		private readonly object sync = new object();
		private readonly IDictionary<string, IChannelGroup> inbound = new Dictionary<string, IChannelGroup>();
		private readonly IDictionary<string, IChannelGroup> outbound = new Dictionary<string, IChannelGroup>();
		private readonly ICollection<IChannelConnector> connectors;
		private readonly ChannelGroupFactory factory;
		private bool receiving;
		private bool initialized;
		private bool disposed;
	}
}