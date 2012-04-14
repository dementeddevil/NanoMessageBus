namespace NanoMessageBus.Channels
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
			this.AssignConnectionAddress();
			return new RabbitConnector(this.ConnectionFactory, this.ShutdownTimeout, this.configurations);
		}
		private void AssignConnectionAddress()
		{
			if (this.EndpointAddress == null)
				return;

			this.ConnectionFactory.Endpoint = new AmqpTcpEndpoint(this.EndpointAddress);
			this.AssignAuthenticationInformation();
		}
		private void AssignAuthenticationInformation()
		{
			if (this.ConnectionFactory.UserName != DefaultUserName)
				return;

			var authentication = this.EndpointAddress.UserInfo.Split(Delimiter);
			this.ConnectionFactory.UserName = authentication.Length > 0 ? authentication[UserNameIndex] : null;
			this.ConnectionFactory.Password = authentication.Length > 1 ? authentication[PasswordIndex] : null;
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
		private static readonly char[] Delimiter = ":".ToCharArray();
		private const string DefaultUserName = "guest";
		private const int UserNameIndex = 0;
		private const int PasswordIndex = 1;
	}
}