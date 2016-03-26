﻿using System.Threading.Tasks;

namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Linq;
	using Logging;

	public class DefaultMessagingHost : IMessagingHost
	{
		public virtual IChannelGroup Initialize()
		{
			Log.Info("Initializing host.");
			lock (this._sync)
			{
				Log.Verbose("Entering critical section (Initialize).");
				this.ThrowWhenDisposed();

				if (!this._initialized)
					this.InitializeChannelGroups();

				this._initialized = true;
				Log.Verbose("Exiting critical section (Initialize).");
			}

			Log.Info("Host initialized.");

			return new IndisposableChannelGroup(this._groups.Values.First());
		}
		protected virtual void InitializeChannelGroups()
		{
			Log.Info("Initializing each channel group on each connector.");
			foreach (var connector in this._connectors)
				foreach (var config in connector.ChannelGroups)
					this.AddChannelGroup(config.GroupName, this._factory(connector, config));

			if (this._groups.Count == 0)
			{
				Log.Warn("No channel groups have been configured.");
				throw new ConfigurationErrorsException("No channel groups have been configured.");
			}
		}
		protected virtual void AddChannelGroup(string name, IChannelGroup group)
		{
			Log.Debug(group.DispatchOnly ?
				"Adding dispatch-only channel group '{0}'." : "Adding full-duplex channel group '{0}'", name);

			group.Initialize();
			this._groups[name] = group;
		}

		public virtual IChannelGroup this[string channelGroup]
		{
			get
			{
				if (channelGroup == null)
					throw new ArgumentNullException(nameof(channelGroup));

				Log.Debug("Reference to dispatch-only channel group '{0}' requested.", channelGroup);

				lock (this._sync)
				{
					Log.Verbose("Entering critical section (GetOutboundChannel).");

					this.ThrowWhenDisposed();
					this.ThrowWhenUninitialized();

					IChannelGroup group;
					if (this._groups.TryGetValue(channelGroup, out group))
					{
						Log.Verbose("Exiting critical section (GetOutboundChannel)--group found.");
						return new IndisposableChannelGroup(group);
					}

					Log.Verbose("Exiting critical section (GetOutboundChannel)--key not found.");
					throw new KeyNotFoundException("Could not find a dispatch-only channel group from the key provided.");
				}
			}
		}

		public virtual void BeginReceive(Func<IDeliveryContext, Task> callback)
		{
			if (callback == null)
				throw new ArgumentNullException(nameof(callback));

			Log.Info("Attempting to begin receive operations.");

			lock (this._sync)
			{
				Log.Verbose("Entering critical section (BeginReceive).");
				this.ThrowWhenDisposed();
				this.ThrowWhenUninitialized();
				this.ThrowWhenReceiving();
				this._receiving = true;

				var activated = this._groups.Values.Where(x => !x.DispatchOnly).Count(x =>
				{
					x.BeginReceive(callback);
					return true;
				});

				Log.Info("Receive operations started against {0} channel groups.", activated);
				Log.Verbose("Exiting critical section (BeginReceive).");
			}
		}

		protected virtual void ThrowWhenDisposed()
		{
			if (!this._disposed)
				return;
		
			Log.Warn("The messaging host has been disposed.");
			throw new ObjectDisposedException(typeof(DefaultMessagingHost).Name);
		}
		protected virtual void ThrowWhenUninitialized()
		{
			if (this._initialized)
				return;

			Log.Warn("The messaging host has not been initialized.");
			throw new InvalidOperationException("The host has not been initialized.");
		}
		protected virtual void ThrowWhenReceiving()
		{
			if (!this._receiving)
				return;

			Log.Warn("Already receiving--a callback has been provided.");
			throw new InvalidOperationException("A callback has already been provided.");
		}

		public DefaultMessagingHost(IEnumerable<IChannelConnector> connectors, ChannelGroupFactory factory)
		{
			if (connectors == null)
				throw new ArgumentNullException(nameof(connectors));

			if (factory == null)
				throw new ArgumentNullException(nameof(factory));

			this._connectors = connectors.Where(x => x != null).ToArray();
			if (this._connectors.Count == 0)
				throw new ArgumentException("No connectors provided.", nameof(connectors));

			this._factory = factory;
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

			lock (this._sync)
			{
				Log.Verbose("Entering critical section (Dispose).");
				if (this._disposed)
					return;

				this._disposed = true;

				foreach (var group in this._groups.Values)
					group.TryDispose();

				Log.Info("Disposing {0} messaging infrastructure connectors and their respective connections, if any.",
					this._connectors.Count);

				foreach (var connector in this._connectors)
					connector.TryDispose();

				Log.Verbose("Exiting critical section (Dispose).");
			}

			Log.Info("Host disposed.");
		}

		private static readonly ILog Log = LogFactory.Build(typeof(DefaultMessagingHost));
		private readonly object _sync = new object();
		private readonly IDictionary<string, IChannelGroup> _groups = new Dictionary<string, IChannelGroup>();
		private readonly ICollection<IChannelConnector> _connectors;
		private readonly ChannelGroupFactory _factory;
		private bool _receiving;
		private bool _initialized;
		private bool _disposed;
	}
}