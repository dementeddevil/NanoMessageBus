namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections.Generic;
	using Logging;

	public class AuditChannel : IMessagingChannel
	{
		public virtual string GroupName
		{
			get { throw new NotImplementedException(); }
		}
		public virtual ChannelMessage CurrentMessage
		{
			get { throw new NotImplementedException(); }
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
			throw new NotImplementedException();
		}
		public virtual void Receive(Action<IDeliveryContext> callback)
		{
			throw new NotImplementedException();
		}

		public AuditChannel(IMessagingChannel inner, ICollection<IAuditListener> listeners)
		{
			// TODO: null checks; also, we should never haver zero listeners
			// if we do, the connector shouldn't be decorating the underlying channel
			this.inner = inner;
			this.listeners = listeners;
		}
		~AuditChannel()
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
			throw new NotImplementedException();
		}

		private static readonly ILog Log = LogFactory.Build(typeof(AuditChannel));
		private readonly IMessagingChannel inner;
		private readonly ICollection<IAuditListener> listeners;
	}
}