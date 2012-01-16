namespace NanoMessageBus
{
	using System;

	public class DependencyResolverDeliveryContext : IDeliveryContext, IDisposable
	{
		public ChannelMessage CurrentMessage
		{
			get { return this.context.CurrentMessage; } // TODO: get under test
		}
		public IDependencyResolver CurrentResolver
		{
			get { return this.resolver; }
		}
		public IChannelTransaction CurrentTransaction
		{
			get { return this.context.CurrentTransaction; } // TODO: get under test
		}
		public void Send(ChannelEnvelope envelope)
		{
			this.context.Send(envelope); // TODO: get under test
		}

		public DependencyResolverDeliveryContext(IDeliveryContext context, IDependencyResolver resolver)
		{
			if (context == null)
				throw new ArgumentNullException("context");

			if (resolver == null)
				throw new ArgumentNullException("resolver");

			this.context = context;
			this.resolver = resolver;
		}
		~DependencyResolverDeliveryContext()
		{
			this.Dispose(false);
		}

		public void Dispose()
		{
			this.Dispose(true);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
				this.resolver.Dispose();
		}

		private readonly IDeliveryContext context;
		private readonly IDependencyResolver resolver;
	}
}