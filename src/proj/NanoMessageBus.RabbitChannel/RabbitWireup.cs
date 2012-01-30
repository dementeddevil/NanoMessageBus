namespace NanoMessageBus.RabbitChannel
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Logging;
	using RabbitMQ.Client;

	public class RabbitWireup
	{
		public virtual ICollection<RabbitChannelGroupConfiguration> ChannelGroups
		{
			get { return this.configurations.ToList(); }
		}
		public virtual Uri EndpointAddress { get; private set; }
		public virtual TimeSpan ShutdownTimeout { get; private set; }
		public virtual ConnectionFactory ConnectionFactory { get; private set; }

		public virtual RabbitWireup WithEndpoint(Uri address)
		{
			if (address == null)
				throw new ArgumentNullException("address");

			this.EndpointAddress = address;
			return this;
		}
		public virtual RabbitWireup WithShutdownTimout(TimeSpan timeout)
		{
			if (timeout < TimeSpan.Zero)
				throw new ArgumentException("Timeout cannot be negative.", "timeout");

			this.ShutdownTimeout = timeout;
			return this;
		}
		public virtual RabbitWireup WithConnectionFactory(ConnectionFactory factory)
		{
			if (factory == null)
				throw new ArgumentNullException("factory");

			this.ConnectionFactory = factory;
			return this;
		}
		public virtual RabbitWireup AddChannelGroup(Action<RabbitChannelGroupConfiguration> callback)
		{
			if (callback == null)
				throw new ArgumentNullException("callback");

			var config = new RabbitChannelGroupConfiguration();
			callback(config);
			this.configurations.Add(config);

			return this;
		}
		public virtual RabbitConnector Build()
		{
			if (this.EndpointAddress != null)
				this.ConnectionFactory.Endpoint = new AmqpTcpEndpoint(this.EndpointAddress);

			return new RabbitConnector(this.ConnectionFactory, this.ShutdownTimeout, this.configurations);
		}

		public RabbitWireup()
		{
			this.ShutdownTimeout = DefaultTimeout;
			this.ConnectionFactory = new ConnectionFactory();
		}

		private static readonly ILog Log = LogFactory.Build(typeof(RabbitWireup));
		private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(3);
		private readonly ICollection<RabbitChannelGroupConfiguration> configurations =
			new LinkedList<RabbitChannelGroupConfiguration>();
	}
}