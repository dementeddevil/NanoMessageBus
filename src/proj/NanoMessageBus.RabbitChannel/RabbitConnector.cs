namespace NanoMessageBus.RabbitChannel
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
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
			var config = this.GetChannelGroupConfiguration(channelGroup);
			this.ThrowWhenDisposed();

			lock (this.locker)
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
				config.MessageAdapter,
				config,
				() => new RabbitSubscription(new Subscription(channel, config)));
		}
		protected virtual IModel EstablishChannel()
		{
			IModel channel = null;

			try
			{
				channel = this.EstablishConnection().CreateModel();
				this.InitializeConfigurations(channel);
				return channel;
			}
			catch (PossibleAuthenticationFailureException e)
			{
				this.ShutdownConnection(channel, ConnectionState.Unauthenticated);
				throw new ChannelConnectionException(e.Message, e);
			}
			catch (Exception e)
			{
				this.ShutdownConnection(channel, ConnectionState.Closed);
				throw new ChannelConnectionException(e.Message, e);
			}
		}
		protected virtual IConnection EstablishConnection()
		{
			if (this.connection != null)
				return this.connection;

			this.CurrentState = ConnectionState.Opening;
			this.connection = this.factory.CreateConnection(this.MaxRedirects);
			this.CurrentState = ConnectionState.Open;
			return this.connection;
		}
		protected virtual void InitializeConfigurations(IModel model)
		{
			foreach (var config in this.configuration.Values)
				config.InitializeBroker(model);
		}
		protected virtual void ShutdownConnection(IModel channel, ConnectionState state)
		{
			this.CurrentState = ConnectionState.Closing;

			if (channel != null)
				channel.Dispose();

			if (this.connection != null)
				this.connection.Dispose();

			this.connection = null;
			this.CurrentState = state;
		}

		protected virtual void ThrowWhenDisposed()
		{
			if (this.disposed)
				throw new ObjectDisposedException("RabbitConnector");
		}

		public RabbitConnector(
			ConnectionFactory factory,
			IEnumerable<RabbitChannelGroupConfiguration> configuration) : this()
		{
			if (factory == null)
				throw new ArgumentNullException("factory");

			this.factory = factory;
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
			if (!disposing || this.disposed)
				return;

			lock (this.locker)
			{
				if (this.disposed)
					return;

				this.disposed = true;
				this.ShutdownConnection(null, ConnectionState.Closed);
			}
		}

		private readonly IDictionary<string, RabbitChannelGroupConfiguration> configuration;
		private readonly ConnectionFactory factory;
		private readonly object locker = new object();
		private IConnection connection;
		private bool disposed;
	}
}