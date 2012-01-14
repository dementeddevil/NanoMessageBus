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
			get { return this.current ?? this.resolver; }
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
			// NOTE: context is being ignored, this is because, in all likelihood, it's coming from
			// the underlying channel anyway, which is what we're already wrapping. Even so,
			// it may be that a safer way to do this is to simply wrap the context provided here
			// in some kind of DependencyResolverDeliveryContext object...
			this.channel.Receive(context => this.ReceiveMessage(callback));
		}
		protected virtual void ReceiveMessage(Action<IDeliveryContext> callback)
		{
			try
			{
				this.current = this.resolver.CreateNestedResolver();
				callback(this);
			}
			finally
			{
				this.current.Dispose();
				this.current = null;
			}
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
		private IDependencyResolver current;
	}
}