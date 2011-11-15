namespace NanoMessageBus
{
	using System;

	public class DefaultMessagingHost : IMessagingHost
	{
		public virtual void Initialize()
		{
		}

		public virtual void BeginDispatch(EnvelopeMessage envelope, string channelGroup)
		{
		}
		public virtual void Dispatch(EnvelopeMessage envelope, string channelGroup)
		{
		}

		public virtual void BeginReceive(Action<IMessagingChannel> callback)
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