namespace NanoMessageBus
{
	using System;

	public class DefaultChannelGroup : IChannelGroup
	{
		public virtual string Name
		{
			get { return null; }
		}
		public virtual int ActiveThreads { get; private set; }

		public virtual void Initialize()
		{
		}
		public virtual void BeginDispatch(EnvelopeMessage envelope)
		{
		}
		public virtual void Dispatch(EnvelopeMessage envelope)
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