﻿namespace NanoMessageBus
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
		private void InitializeChannelGroups()
		{
			foreach (var connector in this.connectors)
				foreach (var config in connector.ChannelGroups)
					this.groups[config.ChannelGroup] = this.factory(connector, config);
		}

		private void ThrowWhenDisposed()
		{
			if (this.disposed)
				throw new ObjectDisposedException(typeof(DefaultMessagingHost).Name);
		}

		public virtual void BeginDispatch(EnvelopeMessage envelope, string channelGroup)
		{
		}
		public virtual void Dispatch(EnvelopeMessage envelope, string channelGroup)
		{
		}

		public virtual void BeginReceive(Action<IMessagingChannel> callback)
		{
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
			}
		}

		private readonly IDictionary<string, IChannelGroup> groups = new Dictionary<string, IChannelGroup>();
		private readonly ICollection<IChannelConnector> connectors;
		private readonly ChannelGroupFactory factory;
		private bool initialized;
		private bool disposed;
	}
}