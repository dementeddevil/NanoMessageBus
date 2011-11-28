namespace NanoMessageBus.RabbitChannel
{
	using System;
	using RabbitMQ.Client;
	using Serialization;

	public class RabbitChannelGroupConfiguration : IChannelGroupConfiguration
	{
		public virtual RabbitChannelGroupConfiguration InitializeConnection(IConnection connection)
		{
			return this;
		}
		public virtual RabbitChannelGroupConfiguration ConfigureChannel(IModel channel)
		{
			return this;
		}

		public virtual string LookupRoutingKey(ChannelMessage message)
		{
			return null;
		}

		public virtual string GroupName
		{
			get { return null; }
		}
		public virtual string InputQueue
		{
			get { return null; }
		}
		public virtual bool DispatchOnly
		{
			get { return false; }
		}
		public virtual int MinWorkers
		{
			get { return 0; }
		}
		public virtual int MaxWorkers
		{
			get { return 0; }
		}
		public virtual TimeSpan ReceiveTimeout
		{
			get { return TimeSpan.Zero; }
		}
		public virtual RabbitTransactionType TransactionType
		{
			get { return RabbitTransactionType.None; }
		}
		public virtual int ChannelBuffer
		{
			get { return 0; }
		}
		public virtual PublicationAddress PoisonMessageExchange
		{
			get { return null; }
		}
		public virtual PublicationAddress DeadLetterExchange
		{
			get { return null; } // null is allowed (which drops the message)
		}
		public virtual int MaxAttempts
		{
			get { return 0; }
		}
		public virtual ISerializer Serializer
		{
			get { return null; }
		}
		public virtual string ApplicationId
		{
			get { return null; }
		}
		public virtual Uri ReturnAddress
		{
			get { return null; } // it's very possible for this to null, e.g. send-only endpoints.
		}
	}
}