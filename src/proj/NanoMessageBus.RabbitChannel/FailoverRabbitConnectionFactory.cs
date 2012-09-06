namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using RabbitMQ.Client;
	using RabbitMQ.Client.Exceptions;

	public class FailoverRabbitConnectionFactory : ConnectionFactory
	{
		public virtual IEnumerable<Uri> Endpoints
		{
			get { return this.endpoints; }
		}

		public virtual FailoverRabbitConnectionFactory AddEndpoints(string brokers)
		{
			var split = brokers
				.Split(EndpointDelimiter, StringSplitOptions.RemoveEmptyEntries)
				.Select(x => new Uri(x, UriKind.Absolute));

			if (!string.IsNullOrEmpty(brokers))
				this.AddEndpoints(split);

			return this;
		}
		public virtual FailoverRabbitConnectionFactory AddEndpoints(params Uri[] brokers)
		{
			return this.AddEndpoints((IEnumerable<Uri>)brokers);
		}
		public virtual FailoverRabbitConnectionFactory AddEndpoints(IEnumerable<Uri> brokers)
		{
			(brokers ?? new Uri[0])
				.Where(x => x != null)
				.ToList()
				.ForEach(x => this.AddEndpoint(x));

			return this;
		}
		public virtual bool AddEndpoint(Uri endpoint)
		{
			if (endpoint == null)
				throw new ArgumentNullException("endpoint");

			if (this.endpoints.Contains(endpoint))
				return false;

			this.AssignAuthenticationInformation(endpoint);
			this.endpoints.Add(endpoint);
			return true;
		}
		private void AssignAuthenticationInformation(Uri address)
		{
			if (this.UserName != DefaultUserName || this.Password != DefaultPassword)
				return;

			var authentication = address.UserInfo.Split(AuthenticationDelimiter);
			this.UserName = authentication.Length > 0 ? authentication[UserNameIndex] : null;
			this.Password = authentication.Length > 1 ? authentication[PasswordIndex] : null;
		}

		public virtual FailoverRabbitConnectionFactory RandomizeEndpoints()
		{
			var random = new Random();

			for (var i = this.endpoints.Count - 1; i > 1; i--)
			{
				var next = random.Next(i + 1);
				var item = this.endpoints[next];
				this.endpoints[next] = this.endpoints[i];
				this.endpoints[i] = item;
			}

			return this;
		}

		public override IConnection CreateConnection(int maxRedirects)
		{
			// TODO: expand each endpoint into a list of IPs based upon current DNS lookups, remember to try/catch?
			// TODO: grab ssl from each endpoint to create the AmqpTcpEndpoint, try/catch bad SSL info and have it
			// result in a failed connection wrapped in some kind of configuration errors?

			var brokers = this.endpoints.Select(x => new AmqpTcpEndpoint(x)).ToArray();
			if (brokers.Length == 0)
				brokers = new[] { new AmqpTcpEndpoint(DefaultEndpoint) };

			IDictionary attempts = new Hashtable(), errors = new Hashtable();
			var connection = this.CreateConnection(maxRedirects, attempts, errors, brokers);
			if (connection == null)
				throw new BrokerUnreachableException(attempts, errors);

			return connection;
		}

		private const string DefaultUserName = "guest";
		private const string DefaultPassword = DefaultUserName;
		private const int UserNameIndex = 0;
		private const int PasswordIndex = 1;
		private static readonly Uri DefaultEndpoint = new Uri("amqp://guest:guest@localhost:5672/");
		private static readonly char[] EndpointDelimiter = new[] { '|' };
		private static readonly char[] AuthenticationDelimiter = ":".ToCharArray();
		private readonly IList<Uri> endpoints = new List<Uri>();
	}
}