namespace NanoMessageBus
{
	using System;

	public class IndisposableChannelGroup : IChannelGroup
	{
		public virtual IChannelGroup Inner
		{
			get { return this.inner; }
		}
		public virtual bool DispatchOnly
		{
			get { return this.inner.DispatchOnly; }
		}

		public virtual void Initialize()
		{
			this.inner.Initialize();
		}
		public virtual IMessagingChannel OpenChannel()
		{
			return this.inner.OpenChannel();
		}
		public virtual void BeginReceive(Action<IDeliveryContext> callback)
		{
			this.inner.BeginReceive(callback);
		}
		public virtual bool BeginDispatch(Action<IDispatchContext> callback)
		{
			return this.inner.BeginDispatch(callback);
		}
		
		public IndisposableChannelGroup(IChannelGroup inner)
		{
			if (inner == null)
				throw new ArgumentNullException("inner");

			this.inner = inner;
		}
		~IndisposableChannelGroup()
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
			// no op
		}

		private readonly IChannelGroup inner;
	}
}