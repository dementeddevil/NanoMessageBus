namespace NanoMessageBus.Channels
{
	using System;
	using Logging;

	public class PooledDispatchChannel : IMessagingChannel
	{
		public virtual int State
		{
			get { return 0; }
		}
		public virtual string GroupName
		{
			get { throw new NotImplementedException(); }
		}
		public virtual ChannelMessage CurrentMessage
		{
			get { throw new NotImplementedException(); } // always returns null
		}
		public virtual IDependencyResolver CurrentResolver
		{
			get { throw new NotImplementedException(); }
		}
		public virtual IChannelTransaction CurrentTransaction
		{
			get { throw new NotImplementedException(); }
		}
		public virtual IChannelGroupConfiguration CurrentConfiguration
		{
			get { throw new NotImplementedException(); }
		}

		public virtual IDispatchContext PrepareDispatch(object message = null)
		{
			throw new NotImplementedException();
		}
		public virtual void Send(ChannelEnvelope envelope)
		{
			throw new NotImplementedException();
		}

		public virtual void BeginShutdown()
		{
			// no op
			throw new NotImplementedException();
		}
		public virtual void Receive(Action<IDeliveryContext> callback)
		{
			// not supported
			throw new NotImplementedException();
		}

		public PooledDispatchChannel(IMessagingChannel inner, int state)
		{
			this.inner = inner;
			this.state = state;
		}
		~PooledDispatchChannel()
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
			// TODO: release actual channel back to the pool
			throw new NotImplementedException();
		}

		private static readonly ILog Log = LogFactory.Build(typeof(PooledDispatchChannel));
		private readonly IMessagingChannel inner;
		private readonly int state;
	}
}