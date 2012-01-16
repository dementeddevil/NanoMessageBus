namespace NanoMessageBus
{
	using System;

	public class DependencyResolverChannel : IMessagingChannel
	{
		public virtual ChannelMessage CurrentMessage
		{
			get { return this.channel.CurrentMessage; }
		}
		public virtual IChannelTransaction CurrentTransaction
		{
			get { return this.channel.CurrentTransaction; }
		}
		public virtual IDependencyResolver CurrentResolver
		{
			get { return this.resolver; }
		}

		public virtual void Send(ChannelEnvelope envelope)
		{
			this.channel.Send(envelope);
		}
		public virtual void BeginShutdown()
		{
			this.channel.BeginShutdown();
		}
		public virtual void Receive(Action<IDeliveryContext> callback)
		{
			this.channel.Receive(x =>
			{
				using (var delivery = new DependencyResolverDeliveryContext(x, this.resolver.CreateNestedResolver()))
					callback(delivery);
			});
		}

		public DependencyResolverChannel(IMessagingChannel channel, IDependencyResolver resolver)
		{
			if (channel == null)
				throw new ArgumentNullException("channel");

			if (resolver == null)
				throw new ArgumentNullException("resolver");

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
			if (!disposing)
				return;

			this.channel.Dispose();
			this.resolver.Dispose();
		}

		private readonly IMessagingChannel channel;
		private readonly IDependencyResolver resolver;
	}
}