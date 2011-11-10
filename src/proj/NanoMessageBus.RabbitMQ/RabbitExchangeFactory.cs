namespace NanoMessageBus.RabbitMQ
{
	using System;
	using global::RabbitMQ.Client;

	public class RabbitExchangeFactory
	{
		public virtual RabbitExchangeFactory Named(string value)
		{
			if (string.IsNullOrEmpty(value))
				throw new ArgumentNullException("value");

			this.name = value.Trim();
			return this;
		}

		public virtual RabbitExchangeFactory Direct()
		{
			this.type = ExchangeType.Direct;
			return this;
		}
		public virtual RabbitExchangeFactory Fanout()
		{
			this.type = ExchangeType.Fanout;
			return this;
		}
		public virtual RabbitExchangeFactory Topic()
		{
			this.type = ExchangeType.Topic;
			return this;
		}
		public virtual RabbitExchangeFactory Headers()
		{
			this.type = ExchangeType.Headers;
			return this;
		}

		public virtual RabbitExchangeFactory Transient()
		{
			this.transient = true;
			return this;
		}
		public virtual RabbitExchangeFactory Disposable()
		{
			this.disposable = true;
			return this;
		}

		public virtual void Build()
		{
			if (string.IsNullOrEmpty(this.name))
				throw new InvalidOperationException("The queue name cannot be empty.");

			this.channel.ExchangeDeclare(this.name, this.type, !this.transient, this.disposable, null);
		}

		public RabbitExchangeFactory(object channel)
		{
			this.channel = channel as IModel;
			if (this.channel == null)
				throw new ArgumentNullException("channel");
		}

		private readonly IModel channel;
		private string name;
		private string type;
		private bool transient;
		private bool disposable;
	}
}