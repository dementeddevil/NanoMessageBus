namespace NanoMessageBus
{
	using Autofac;

	public class AutofacChannelMessageHandler : IMessageHandler<ChannelMessage>
	{
		public void Handle(ChannelMessage message)
		{
			this.handler.Handle(message);
		}

		public AutofacChannelMessageHandler(IHandlerContext context, IMessageHandler<ChannelMessage> inner = null)
		{
			var container = context.CurrentResolver.As<ILifetimeScope>();

			var builder = new ContainerBuilder();
			builder.RegisterInstance(context).ExternallyOwned(); // single instance for this and descendent scopes
			builder.Update(container.ComponentRegistry);

			this.handler = inner ?? new DefaultChannelMessageHandler(context, container.Resolve<IRoutingTable>());
		}

		private readonly IMessageHandler<ChannelMessage> handler;
	}
}