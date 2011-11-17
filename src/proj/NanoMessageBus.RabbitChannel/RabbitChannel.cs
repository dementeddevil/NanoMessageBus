namespace NanoMessageBus.RabbitChannel
{
	using System;

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
		public virtual void Receive(Action<IMessagingChannel> callback)
		{
		}
		public virtual void Send(ChannelMessage message, params Uri[] destinations)
		{
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
		}
	}
}