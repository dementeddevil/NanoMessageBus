namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Linq;
	using Logging;

	public class DefaultMessagingHost : IMessagingHost
	{
		public virtual void Initialize()
		{
			Log.Info("Initializing host.");
			lock (this.sync)
			{
				Log.Verbose("Entering critical section (Initialize).");
				this.ThrowWhenDisposed();

				if (!this.initialized)
					this.InitializeChannelGroups();

				this.initialized = true;
				Log.Verbose("Exiting critical section (Initialize).");
			}
			Log.Info("Host initialized.");
		}
		protected virtual void InitializeChannelGroups()
		{
			Log.Info("Initializing each channel group on each connector.");
			foreach (var connector in this.connectors)
				foreach (var config in connector.ChannelGroups)
					this.AddChannelGroup(config.GroupName, this.factory(connector, config));

			if (this.groups.Count == 0)
				throw new ConfigurationErrorsException("No channel groups have been configured.");
		}
		protected virtual void AddChannelGroup(string name, IChannelGroup group)
		{
			Log.Debug(group.DispatchOnly ?
				"Adding dispatch-only channel group '{0}'." : "Adding full-duplex channel group '{0}'", name);

			group.Initialize();
			this.groups[name] = group;
		}

		public virtual IChannelGroup this[string channelGroup]
		{
			get
			{
				if (channelGroup == null)
					throw new ArgumentNullException("channelGroup");

				Log.Debug("Reference to dispatch-only channel group '{0}' requested.", channelGroup);

				lock (this.sync)
				{
					Log.Verbose("Entering critical section (GetOutboundChannel).");

					this.ThrowWhenDisposed();
					this.ThrowWhenUninitialized();

					IChannelGroup group;
					if (this.groups.TryGetValue(channelGroup, out group))
					{
						Log.Verbose("Exiting critical section (GetOutboundChannel)--group found.");
						return group;
					}

					Log.Verbose("Exiting critical section (GetOutboundChannel)--key not found.");
					throw new KeyNotFoundException("Could not find a dispatch-only channel group from the key provided.");
				}
			}
		}

		public virtual void BeginReceive(Action<IDeliveryContext> callback)
		{
			if (callback == null)
				throw new ArgumentNullException("callback");

			Log.Info("Attempting to begin receive operations.");

			lock (this.sync)
			{
				Log.Verbose("Entering critical section (Dispose).");
				this.ThrowWhenDisposed();
				this.ThrowWhenUninitialized();
				this.ThrowWhenReceiving();
				this.receiving = true;

				var activated = this.groups.Values.Where(x => !x.DispatchOnly).Count(x =>
				{
					x.BeginReceive(callback);
					return true;
				});

				Log.Info("Receive operations started against {0} channel groups.", activated);
				Log.Verbose("Exiting critical section (Dispose).");
			}
		}

		protected virtual void ThrowWhenDisposed()
		{
			if (!this.disposed)
				return;
		
			Log.Warn("The messaging host has been disposed.");
			throw new ObjectDisposedException(typeof(DefaultMessagingHost).Name);
		}
		protected virtual void ThrowWhenUninitialized()
		{
			if (this.initialized)
				return;

			Log.Warn("The messaging host has not been initialized.");
			throw new InvalidOperationException("The host has not been initialized.");
		}
		protected virtual void ThrowWhenReceiving()
		{
			if (!this.receiving)
				return;

			Log.Warn("Already receiving--a callback has been provided.");
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

			Log.Info("Disposing host.");

			lock (this.sync)
			{
				Log.Verbose("Entering critical section (Dispose).");
				if (this.disposed)
					return;

				this.disposed = true;

				foreach (var group in this.groups.Values)
					group.Dispose();

				Log.Info("Disposing {0} messaging infrastructure connectors and their respective connections, if any.",
					this.connectors.Count);

				foreach (var connector in this.connectors)
					connector.Dispose();

				Log.Verbose("Exiting critical section (Dispose).");
			}

			Log.Info("Host disposed.");
		}

		private static readonly ILog Log = LogFactory.Build(typeof(DefaultMessagingHost));
		private readonly object sync = new object();
		private readonly IDictionary<string, IChannelGroup> groups = new Dictionary<string, IChannelGroup>();
		private readonly ICollection<IChannelConnector> connectors;
		private readonly ChannelGroupFactory factory;
		private bool receiving;
		private bool initialized;
		private bool disposed;
	}
}