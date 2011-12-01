namespace NanoMessageBus.RabbitChannel
{
	using System;
	using RabbitMQ.Client;
	using Serialization;

	public class RabbitChannelGroupConfiguration : IChannelGroupConfiguration
	{
		public virtual void InitializeBroker(IModel channel)
		{
		}
		public virtual string LookupRoutingKey(ChannelMessage message)
		{
			return null;
		}

		public virtual string GroupName { get; private set; }
		public virtual string InputQueue { get; private set; }
		public virtual Uri ReturnAddress { get; private set; } // it's very possible for this to null, e.g. send-only endpoints.
		public virtual bool DispatchOnly { get; private set; }
		public virtual int MinWorkers { get; private set; }
		public virtual int MaxWorkers { get; private set; }
		public virtual TimeSpan ReceiveTimeout { get; private set; }
		public virtual RabbitTransactionType TransactionType { get; private set; }
		public virtual int ChannelBuffer { get; private set; }
		public virtual PublicationAddress PoisonMessageExchange { get; private set; }
		public virtual PublicationAddress DeadLetterExchange { get; private set; } // null is allowed (which drops the message)
		public virtual int MaxAttempts { get; private set; }
		public virtual ISerializer Serializer { get; private set; }
		public virtual string ApplicationId { get; private set; }
		public virtual RabbitMessageAdapter MessageAdapter { get; private set; }

		public virtual RabbitChannelGroupConfiguration WithGroupName(string name)
		{
			if (name == null)
				throw new ArgumentNullException("name");

			this.GroupName = name;
			return this;
		}
		public virtual RabbitChannelGroupConfiguration WithReceiveTimeout(TimeSpan timeout)
		{
			if (timeout < TimeSpan.Zero)
				throw new ArgumentException("Timeout must be positive", "timeout");

			this.ReceiveTimeout = timeout;
			return this;
		}
		public virtual RabbitChannelGroupConfiguration WithWorkers(int min, int max)
		{
			if (min <= 0)
				throw new ArgumentException("At least one worker must be specified.", "min");

			if (min > max)
				throw new ArgumentException(
					"The maximum workers specified must be at least the same as the minimum specified.", "max");

			this.MinWorkers = min;
			this.MaxWorkers = max;

			return this;
		}
		public virtual RabbitChannelGroupConfiguration WithInputQueue(string name)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");

			this.ReturnAddress = new Uri(DefaultReturnAddressFormat.FormatWith(name));
			this.InputQueue = name;
			this.DispatchOnly = false;

			return this;
		}
		public virtual RabbitChannelGroupConfiguration WithDispatchOnly()
		{
			this.DispatchOnly = true;
			this.InputQueue = null;

			return this;
		}
		public virtual RabbitChannelGroupConfiguration WithTransaction(RabbitTransactionType transaction)
		{
			this.TransactionType = transaction;
			return this;
		}
		public virtual RabbitChannelGroupConfiguration WithChannelBuffer(int maxMessageBufer)
		{
			if (maxMessageBufer < 0)
				throw new ArgumentException("A non-negative buffer size is required.", "maxMessageBufer");

			this.ChannelBuffer = maxMessageBufer;
			return this;
		}
		public virtual RabbitChannelGroupConfiguration WithReturnAddress(Uri address)
		{
			if (address == null)
				throw new ArgumentNullException("address");

			this.ReturnAddress = address;
			return this;
		}
		public virtual RabbitChannelGroupConfiguration WithPoisonMessageExchange(string exchange)
		{
			if (exchange == null)
				throw new ArgumentNullException("exchange");

			this.PoisonMessageExchange = new PublicationAddress(ExchangeType.Fanout, exchange, string.Empty);
			return this;
		}
		public virtual RabbitChannelGroupConfiguration WithDeadLetterExchange(string exchange)
		{
			if (exchange == null)
				throw new ArgumentNullException("exchange");

			this.DeadLetterExchange = new PublicationAddress(ExchangeType.Fanout, exchange, string.Empty);
			return this;
		}
		public virtual RabbitChannelGroupConfiguration WithMaxAttempts(int attempts)
		{
			if (attempts <= 0)
				throw new ArgumentException("The maximum number of attempts must be positive", "attempts");

			this.MaxAttempts = attempts;
			return this;
		}
		public virtual RabbitChannelGroupConfiguration WithApplicationId(string identifier)
		{
			if (identifier == null)
				throw new ArgumentNullException("identifier");

			this.ApplicationId = identifier.Trim();
			return this;
		}
		public virtual RabbitChannelGroupConfiguration WithSerializer(ISerializer serializer)
		{
			if (serializer == null)
				throw new ArgumentNullException("serializer");

			this.Serializer = serializer;
			return this;
		}

		public RabbitChannelGroupConfiguration()
		{
			this.ApplicationId = DefaultAppId;
			this.ReceiveTimeout = DefaultReceiveTimeout;
			this.MinWorkers = this.MaxWorkers = DefaultWorkerCount;
			this.ChannelBuffer = DefaultChannelBuffer;
			this.MaxAttempts = DefaultMaxAttempts;
			this.PoisonMessageExchange = new PublicationAddress(
				ExchangeType.Fanout, DefaultPoisonMessageExchange, string.Empty);
			this.MessageAdapter = new RabbitMessageAdapter(this);
			this.Serializer = DefaultSerializer;
		}

		private const int DefaultWorkerCount = 1;
		private const int DefaultMaxAttempts = 1;
		private const int DefaultChannelBuffer = 1024;
		private const string DefaultReturnAddressFormat = "direct://default/my-queue-name";
		private const string DefaultPoisonMessageExchange = "poison-messages";
		private const string DefaultAppId = "rabbit-endpoint";
		private static readonly TimeSpan DefaultReceiveTimeout = TimeSpan.FromMilliseconds(500);
		private static readonly ISerializer DefaultSerializer = new BinarySerializer();
	}
}