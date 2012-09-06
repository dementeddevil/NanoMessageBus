namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RabbitMQ.Client;

	public class RabbitWireup
	{
		public virtual ICollection<RabbitChannelGroupConfiguration> ChannelGroups
		{
			get { return this.configurations.ToList(); }
		}
		public virtual TimeSpan ShutdownTimeout { get; private set; }
		public virtual FailoverConnectionFactory ConnectionFactory { get; private set; }

		public virtual RabbitWireup WithShutdownTimout(TimeSpan timeout)
		{
			if (timeout < TimeSpan.Zero)
				throw new ArgumentException("Timeout cannot be negative.", "timeout");

			this.ShutdownTimeout = timeout;
			return this;
		}
		public virtual RabbitWireup WithConnectionFactory(FailoverConnectionFactory factory)
		{
			if (factory == null)
				throw new ArgumentNullException("factory");

			this.ConnectionFactory = factory;
			return this;
		}
		public virtual RabbitWireup WithCertificateAuthentication()
		{
			this.ConnectionFactory.AuthMechanisms = new AuthMechanismFactory[]
			{
				new ExternalMechanismFactory(), 
				new PlainMechanismFactory()
			};
			return this;
		}
		public virtual RabbitWireup AddEndpoint(Uri address, bool ordered = false)
		{
			if (address == null)
				throw new ArgumentNullException("address");

			this.ConnectionFactory.AddEndpoint(address);
			if (!ordered)
				this.ConnectionFactory.RandomizeEndpoints();

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
			return new RabbitConnector(this.ConnectionFactory, this.ShutdownTimeout, this.configurations);
		}

		public RabbitWireup()
		{
			this.ShutdownTimeout = DefaultTimeout;
			this.ConnectionFactory = new FailoverConnectionFactory();
		}

		private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(3);
		private readonly ICollection<RabbitChannelGroupConfiguration> configurations =
			new LinkedList<RabbitChannelGroupConfiguration>();
	}
}