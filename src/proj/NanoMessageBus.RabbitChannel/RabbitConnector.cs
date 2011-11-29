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
			var group = this.GetChannelGroup(channelGroup);
			lock (this.locker)
			{
				var channel = this.EstablishChannel();
				return new RabbitChannel(
					channel,
					group.MessageAdapter,
					group,
					() => new RabbitSubscription(new Subscription(channel, group)));
			}
		}
		protected virtual RabbitChannelGroupConfiguration GetChannelGroup(string channelGroup)
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
				this.ShutdownConnection(channel);
				this.CurrentState = ConnectionState.Unauthenticated;
				throw new ChannelConnectionException(e.Message, e);
			}
			catch (Exception e)
			{
				this.ShutdownConnection(channel);
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
		protected virtual void ShutdownConnection(IModel channel)
		{
			this.CurrentState = ConnectionState.Closing;

			if (channel != null)
				channel.Dispose();

			if (this.connection != null)
				this.connection.Dispose();

			this.connection = null;
			this.CurrentState = ConnectionState.Closed;
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
			if (!disposing)
				return;
		}

		private readonly IDictionary<string, RabbitChannelGroupConfiguration> configuration;
		private readonly ConnectionFactory factory;
		private readonly object locker = new object();
		private IConnection connection;
	}
}