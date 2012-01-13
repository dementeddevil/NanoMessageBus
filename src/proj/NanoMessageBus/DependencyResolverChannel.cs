namespace NanoMessageBus
{
	using System;

	public class DependencyResolverChannel : IMessagingChannel
	{
		public ChannelMessage CurrentMessage
		{
			get { return null; }
		}
		public IChannelTransaction CurrentTransaction
		{
			get { return null; }
		}
		public IDependencyResolver CurrentResolver
		{
			get { return null; }
		}

		public void Send(ChannelEnvelope envelope)
		{
		}
		public void BeginShutdown()
		{
		}
		public void Receive(Action<IDeliveryContext> callback)
		{
		}

		public DependencyResolverChannel(IMessagingChannel channel, IDependencyResolver resolver)
		{
			this.channel = channel;
			this.resolver = resolver;
		}
		~DependencyResolverChannel()
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

		private readonly IMessagingChannel channel;
		private readonly IDependencyResolver resolver;
		private IDependencyResolver current;
	}
}