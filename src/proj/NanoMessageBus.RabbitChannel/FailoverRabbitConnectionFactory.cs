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
				.Split(Delimiter, StringSplitOptions.RemoveEmptyEntries)
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

			this.endpoints.Add(endpoint);
			return true;
		}

		public FailoverRabbitConnectionFactory RandomizeEndpoints()
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
			var brokers = this.endpoints.Select(x => new AmqpTcpEndpoint(x)).ToArray();
			if (brokers.Length == 0)
				brokers = new[] { new AmqpTcpEndpoint(DefaultEndpoint) };

			IDictionary attempts = new Hashtable(), errors = new Hashtable();
			var connection = this.CreateConnection(maxRedirects, attempts, errors, brokers);
			if (connection == null)
				throw new BrokerUnreachableException(attempts, errors);

			return connection;
		}

		private static readonly Uri DefaultEndpoint = new Uri("amqp://guest:guest@localhost:5672/");
		private static readonly char[] Delimiter = new[] { '|' };
		private readonly IList<Uri> endpoints = new List<Uri>();
	}
}