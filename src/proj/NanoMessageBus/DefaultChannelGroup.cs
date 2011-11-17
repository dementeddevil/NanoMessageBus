namespace NanoMessageBus
{
	using System;

	public class DefaultChannelGroup : IChannelGroup
	{
		public virtual void Initialize()
		{
		}

		public virtual void BeginDispatch(ChannelMessage message)
		{
		}
		public virtual void Dispatch(ChannelMessage message)
		{
		}

		public virtual void BeginReceive(Action<IMessagingChannel> callback)
		{
		}

		public DefaultChannelGroup()
		{
		}
		~DefaultChannelGroup()
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
	}
}