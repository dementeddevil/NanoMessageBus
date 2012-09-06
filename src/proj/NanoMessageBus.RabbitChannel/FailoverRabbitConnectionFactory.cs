namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using RabbitMQ.Client;
	using RabbitMQ.Client.Exceptions;

	public class FailoverRabbitConnectionFactory : ConnectionFactory
	{
		public virtual IEnumerable<Uri> Endpoints
		{
			get { return this.brokers; }
		}

		public virtual FailoverRabbitConnectionFactory AddEndpoints(string endpoint)
		{
			var split = endpoint
				.Split(EndpointDelimiter, StringSplitOptions.RemoveEmptyEntries)
				.Select(x => new Uri(x, UriKind.Absolute));

			if (!string.IsNullOrEmpty(endpoint))
				this.AddEndpoints(split);

			return this;
		}
		public virtual FailoverRabbitConnectionFactory AddEndpoints(params Uri[] endpoints)
		{
			return this.AddEndpoints((IEnumerable<Uri>)endpoints);
		}
		public virtual FailoverRabbitConnectionFactory AddEndpoints(IEnumerable<Uri> endpoints)
		{
			(endpoints ?? new Uri[0])
				.Where(x => x != null)
				.ToList()
				.ForEach(x => this.AddEndpoint(x));

			return this;
		}

		public virtual bool AddEndpoint(Uri endpoint)
		{
			if (endpoint == null)
				throw new ArgumentNullException("endpoint");

			if (this.brokers.Contains(endpoint))
				return false;

			this.brokers.Add(endpoint);
			if (this.UserName != DefaultUserName || this.Password != DefaultPassword)
				return true;

			var authentication = endpoint.UserInfo.Split(AuthenticationDelimiter);
			this.UserName = authentication.Length > 0 ? authentication[UserNameIndex] : null;
			this.Password = authentication.Length > 1 ? authentication[PasswordIndex] : null;

			return true;
		}

		public virtual FailoverRabbitConnectionFactory RandomizeEndpoints()
		{
			var random = new Random();

			for (var i = this.brokers.Count - 1; i > 1; i--)
			{
				var next = random.Next(i + 1);
				var item = this.brokers[next];
				this.brokers[next] = this.brokers[i];
				this.brokers[i] = item;
			}

			return this;
		}

		public override IConnection CreateConnection(int maxRedirects)
		{
			var endpoints = this.brokers.SelectMany(this.ToAmqpEndpoint).ToArray();
			if (endpoints.Length == 0)
				endpoints = new[] { new AmqpTcpEndpoint(DefaultEndpoint) };

			IDictionary attempts = new Hashtable(), errors = new Hashtable();
			var connection = this.CreateConnection(maxRedirects, attempts, errors, endpoints);
			if (connection == null)
				throw new BrokerUnreachableException(attempts, errors);

			return connection;
		}
		private IEnumerable<AmqpTcpEndpoint> ToAmqpEndpoint(Uri endpoint)
		{
			// TODO: grab ssl from each endpoint to create the AmqpTcpEndpoint, try/catch bad SSL info and have it
			// result in a failed connection wrapped in some kind of configuration errors?
			return this.nslookup(endpoint.Host)
				.Select(x => new UriBuilder(endpoint.Scheme, x.ToString(), endpoint.Port, endpoint.PathAndQuery, endpoint.Fragment))
				.Select(x => new AmqpTcpEndpoint(x.Uri));
		}

		public FailoverRabbitConnectionFactory() : this(null)
		{
		}
		public FailoverRabbitConnectionFactory(Func<string, ICollection<IPAddress>> nslookup)
		{
			this.nslookup = nslookup ?? Dns.GetHostAddresses;
		}

		private const string DefaultUserName = "guest";
		private const string DefaultPassword = DefaultUserName;
		private const int UserNameIndex = 0;
		private const int PasswordIndex = 1;
		private static readonly Uri DefaultEndpoint = new Uri("amqp://guest:guest@localhost:5672/");
		private static readonly char[] EndpointDelimiter = new[] { '|' };
		private static readonly char[] AuthenticationDelimiter = ":".ToCharArray();
		private readonly IList<Uri> brokers = new List<Uri>();
		private readonly Func<string, ICollection<IPAddress>> nslookup;
	}
}