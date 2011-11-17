namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class DefaultMessagingHost : IMessagingHost
	{
		public virtual void Initialize()
		{
			lock (this.groups)
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
					this.groups[config.ChannelGroup] = this.factory(connector, config);
		}

		public virtual void BeginDispatch(string channelGroup, ChannelMessage message)
		{
			this.Dispatch(channelGroup, message, false);
		}
		public virtual void Dispatch(string channelGroup, ChannelMessage message)
		{
			this.Dispatch(channelGroup, message, true);
		}
		protected virtual void Dispatch(string channelGroup, ChannelMessage message, bool sync)
		{
			if (message == null)
				throw new ArgumentNullException("message");
			if (channelGroup == null)
				throw new ArgumentNullException("channelGroup");

			lock (this.groups)
			{
				this.ThrowWhenUninitialized();
				this.ThrowWhenDisposed();

				IChannelGroup group;
				if (!this.groups.TryGetValue(channelGroup, out group))
					throw new KeyNotFoundException("The key for the channel group provided was not found.");

				if (sync)
					group.Dispatch(message);
				else
					group.BeginDispatch(message);
			}
		}

		public virtual void BeginReceive(Action<IMessagingChannel> callback)
		{
			if (callback == null)
				throw new ArgumentNullException("callback");

			lock (this.groups)
			{
				this.ThrowWhenDisposed();
				this.ThrowWhenUninitialized();
				this.ThrowWhenReceiving();
				this.receiving = true;

				foreach (var group in this.groups.Values)
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

			lock (this.groups)
			{
				if (this.disposed)
					return;

				this.disposed = true;

				foreach (var group in this.groups.Values)
					group.Dispose(); // TODO: make sure this doesn't wrap around and result in a deadlock

				this.groups.Clear();
			}
		}

		private readonly IDictionary<string, IChannelGroup> groups = new Dictionary<string, IChannelGroup>();
		private readonly ICollection<IChannelConnector> connectors;
		private readonly ChannelGroupFactory factory;
		private bool receiving;
		private bool initialized;
		private bool disposed;
	}
}