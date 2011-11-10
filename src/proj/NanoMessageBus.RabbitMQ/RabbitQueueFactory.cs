namespace NanoMessageBus.RabbitMQ
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using global::RabbitMQ.Client;

	public class RabbitQueueFactory
	{
		public virtual RabbitQueueFactory Named(string value)
		{
			if (string.IsNullOrEmpty(value))
				throw new ArgumentNullException("value");

			this.name = value.Trim();
			return this;
		}

		public virtual RabbitQueueFactory Transient()
		{
			this.transient = true;
			return this;
		}
		public virtual RabbitQueueFactory ExclusiveToConsumer()
		{
			this.exclusive = true;
			return this;
		}
		public virtual RabbitQueueFactory Disposable()
		{
			this.disposable = true;
			return this;
		}
		public virtual RabbitQueueFactory Purge()
		{
			this.purge = true;
			return this;
		}

		public virtual RabbitQueueFactory Bind(string exchange, params string[] keys)
		{
			return this.Bind(exchange, keys as ICollection<string>);
		}
		public virtual RabbitQueueFactory Bind(string exchange, ICollection<string> keys)
		{
			if (string.IsNullOrEmpty(exchange))
				throw new ArgumentNullException("exchange");

			if (keys == null || keys.Count == 0)
				throw new ArgumentNullException("keys");

			ICollection<string> list;
			if (!this.routes.TryGetValue(exchange, out list))
				this.routes[exchange] = list = new LinkedList<string>();

			foreach (var key in keys)
				list.Add(key);

			return this;
		}

		public virtual void Build()
		{
			if (string.IsNullOrEmpty(this.name))
				throw new InvalidOperationException("The queue name cannot be empty.");

			// TODO: try/catch?
			// TODO: as an added precaution, we queue make a 'queue' object (and 'exchange' object)
			// which, when disposed, invoke the appropriate channel cleanup code???
			this.DeclareQueue();
			this.DeclarePrivateExchange();
			this.Clear();
			this.Bind();
		}
		private void DeclareQueue()
		{
			this.channel.QueueDeclare(this.name, !this.transient, this.exclusive, this.disposable, null);
		}
		private void DeclarePrivateExchange()
		{
			var exchange = PrivateExchangeFormat.FormatWith(this.name);
			this.channel.ExchangeDeclare(exchange, ExchangeType.Direct, !this.transient, this.disposable, null);
			this.channel.QueueBind(this.name, exchange, string.Empty);
		}
		private void Clear()
		{
			if (this.purge)
				this.channel.QueuePurge(this.name);
		}
		private void Bind()
		{
			foreach (var item in this.routes)
				foreach (var key in item.Value)
					this.channel.QueueBind(this.name, item.Key, key);
		}

		public RabbitQueueFactory(object channel)
		{
			this.channel = channel as IModel;
			if (this.channel == null)
				throw new ArgumentNullException("channel");
		}

		private const string PrivateExchangeFormat = "private-{0}";
		private readonly IDictionary<string, ICollection<string>> routes = new Dictionary<string, ICollection<string>>();
		private readonly IModel channel;
		private string name;
		private bool transient;
		private bool exclusive;
		private bool disposable;
		private bool purge;
	}
}