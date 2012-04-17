namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using Logging;
	using RabbitMQ.Client;
	using RabbitMQ.Client.Exceptions;

	public class RabbitConnector : IChannelConnector
	{
		public virtual ConnectionState CurrentState { get; private set; }
		public virtual int MaxRedirects { get; set; }
		public virtual IEnumerable<IChannelGroupConfiguration> ChannelGroups
		{
			get { return this.configuration.Values; }
		}

		public virtual IMessagingChannel Connect(string channelGroup)
		{
			Log.Debug("Attempting to establish channel for group '{0}'.", channelGroup);
			var config = this.GetChannelGroupConfiguration(channelGroup);
			this.ThrowWhenDisposed();

			lock (this.sync)
				return this.EstablishChannel(config);
		}
		protected virtual RabbitChannelGroupConfiguration GetChannelGroupConfiguration(string channelGroup)
		{
			if (channelGroup == null)
				throw new ArgumentNullException("channelGroup");

			if (string.IsNullOrEmpty(channelGroup))
				throw new ArgumentException("No channel group specified.", "channelGroup");

			RabbitChannelGroupConfiguration config;
			if (!this.configuration.TryGetValue(channelGroup, out config))
				throw new KeyNotFoundException("The desired channel group was not found.");

			return config;
		}
		protected virtual IMessagingChannel EstablishChannel(RabbitChannelGroupConfiguration config)
		{
			this.ThrowWhenDisposed();

			var channel = this.EstablishChannel();
			return new RabbitChannel(
				channel,
				config,
				() => new RabbitSubscription(new Subscription(channel, config)));
		}
		protected virtual IModel EstablishChannel()
		{
			IModel channel = null;

			try
			{
				if (this.connection != null)
				{
					Log.Debug("Creating channel from existing connection.");
					return this.connection.CreateModel();
				}

				Log.Debug("Attempting to establish a new connection to the messaging infrastructure.");
				this.CurrentState = ConnectionState.Opening;
				this.connection = this.factory.CreateConnection(this.MaxRedirects);
				this.CurrentState = ConnectionState.Open;

				channel = this.connection.CreateModel();
				this.InitializeConfigurations(channel);
			}
			catch (PossibleAuthenticationFailureException e)
			{
				Log.Warn("Invalid security credentials.");
				this.Close(channel, ConnectionState.Unauthenticated, e);
			}
			catch (OperationInterruptedException e)
			{
				var shutdownCode = e.ShutdownReason == null ? 0 : e.ShutdownReason.ReplyCode;
				if (shutdownCode == PreconditionFailed)
				{
					Log.Fatal("Attempting to redefine existing queue/exchange with different parameters; unable to continue.");
					throw new ChannelConfigurationException(e.Message, e);
				}
				if (shutdownCode == ResourceLocked)
				{
					Log.Fatal("Attempting to access a queue locked exclusively by another consumer; unable to continue.");
					throw new ChannelConfigurationException(e.Message, e);
				}

				Log.Info("Connection attempt interrupted; socked closed.");
				this.Close(channel, ConnectionState.Disconnected, e);
			}
			catch (IOException e)
			{
				Log.Info("Connection attempt failed; socket aborted.");
				this.Close(channel, ConnectionState.Disconnected, e);
			}
			catch (Exception e)
			{
				Log.Info("Unhandled exception, socket aborted.", e);
				this.Close(channel, ConnectionState.Closed, e);
			}

			return channel;
		}
		protected virtual void InitializeConfigurations(IModel model)
		{
			foreach (var config in this.configuration.Values)
			{
				Log.Debug("Initializing the messaging infrastructure.");
				config.ConfigureChannel(model);
			}
		}

		protected virtual void ThrowWhenDisposed()
		{
			if (!this.disposed)
				return;

			Log.Warn("The channel connector has been disposed.");
			throw new ObjectDisposedException(typeof(RabbitConnector).Name);
		}

		public RabbitConnector(
			ConnectionFactory factory,
			TimeSpan shutdownTimeout,
			IEnumerable<RabbitChannelGroupConfiguration> configuration) : this()
		{
			if (factory == null)
				throw new ArgumentNullException("factory");

			this.factory = factory;
			this.shutdownTimeout = (int)shutdownTimeout.TotalMilliseconds;
			this.configuration = (configuration ?? new RabbitChannelGroupConfiguration[0])
				.Where(x => x != null)
				.Where(x => !string.IsNullOrEmpty(x.GroupName))
				.ToDictionary(x => x.GroupName ?? string.Empty, x => x);

			if (this.configuration.Count == 0)
				throw new ArgumentException("No configurations provided.", "configuration");
		}
		protected RabbitConnector()
		{
		}
		~RabbitConnector()
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

				Log.Verbose("Disposing any active connection(s).");

				this.disposed = true;
				this.Close(null, ConnectionState.Closed);

				Log.Debug("Connection disposed.");
			}
		}
		protected virtual void Close(IModel channel, ConnectionState state, Exception exception = null)
		{
			this.CurrentState = ConnectionState.Closing;

			if (channel != null)
			{
				Log.Debug("Aborting operations on temporary channel.");
				channel.Abort();
			}

			if (this.connection != null)
			{
				Log.Debug("Blocking up to {0} ms before forcing the existing connection to close.", this.shutdownTimeout);

				// calling connection.TryDispose() can thrown while connection.Abort() closes without throwing
				this.connection.Abort(this.shutdownTimeout);
			}

			this.connection = null;
			this.CurrentState = state;

			if (exception != null)
				throw new ChannelConnectionException(exception.Message, exception);
		}

		private const int PreconditionFailed = 406;
		private const int ResourceLocked = 405;
		private static readonly ILog Log = LogFactory.Build(typeof(RabbitConnector));
		private readonly IDictionary<string, RabbitChannelGroupConfiguration> configuration;
		private readonly ConnectionFactory factory;
		private readonly int shutdownTimeout;
		private readonly object sync = new object();
		private IConnection connection;
		private bool disposed;
	}
}