namespace NanoMessageBus.RabbitChannel
{
	using System;
	using RabbitMQ.Client;

	public class RabbitChannel : IMessagingChannel
	{
		public virtual string ChannelGroup
		{
			get { return null; }
		}
		public virtual ChannelMessage CurrentMessage
		{
			get { return null; }
		}
		public virtual IChannelTransaction CurrentTransaction
		{
			get { return null; }
		}
		public virtual void Receive(Action<IDeliveryContext> callback)
		{
		}
		public virtual void Send(ChannelEnvelope envelope)
		{
		}

		public RabbitChannel(IModel channel, RabbitTransactionType transactionType)
		{
			this.channel = channel;
			this.transactionType = transactionType;
		}
		~RabbitChannel()
		{
			this.Dispose(false);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
		}

		private readonly IModel channel;
		private readonly RabbitTransactionType transactionType;
	}
}