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
				config,
				() => new RabbitSubscription(new Subscription(channel, config)));
		}
		protected virtual IModel EstablishChannel()
		{
			IModel channel = null;

			try
			{
				if (this.connection != null)
					return this.connection.CreateModel();

				this.CurrentState = ConnectionState.Opening;
				this.connection = this.factory.CreateConnection(this.MaxRedirects);
				this.CurrentState = ConnectionState.Open;

				channel = this.connection.CreateModel();
				this.InitializeConfigurations(channel);
			}
			catch (PossibleAuthenticationFailureException e)
			{
				this.Close(channel, ConnectionState.Unauthenticated, e);
			}
			catch (OperationInterruptedException e)
			{
				this.Close(channel, ConnectionState.Disconnected, e);
			}
			catch (Exception e)
			{
				this.Close(channel, ConnectionState.Closed, e);
			}

			return channel;
		}
		protected virtual void InitializeConfigurations(IModel model)
		{
			foreach (var config in this.configuration.Values)
				config.ConfigureChannel(model);
		}

		protected virtual void ThrowWhenDisposed()
		{
			if (this.disposed)
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

			lock (this.locker)
			{
				if (this.disposed)
					return;

				this.disposed = true;
				this.Close(null, ConnectionState.Closed);
			}
		}
		protected virtual void Close(IModel channel, ConnectionState state, Exception exception = null)
		{
			this.CurrentState = ConnectionState.Closing;

			if (channel != null)
				channel.Abort();

			// dispose can throw while abort does the exact same thing without throwing
			if (this.connection != null)
				this.connection.Abort(this.shutdownTimeout);

			this.connection = null;
			this.CurrentState = state;

			if (exception != null)
				throw new ChannelConnectionException(exception.Message, exception);
		}

		private readonly IDictionary<string, RabbitChannelGroupConfiguration> configuration;
		private readonly ConnectionFactory factory;
		private readonly int shutdownTimeout;
		private readonly object locker = new object();
		private IConnection connection;
		private bool disposed;
	}
}