namespace NanoMessageBus
{
	using System;
	using Autofac;

	public class AutofacChannelMessageHandler : IMessageHandler<ChannelMessage>
	{
		public void Handle(ChannelMessage message)
		{
			this.handler.Handle(message);
		}

		public AutofacChannelMessageHandler(
			IHandlerContext context,
			IRoutingTable table,
			IMessageHandler<ChannelMessage> inner = null)
		{
			if (context == null)
				throw new ArgumentNullException("context");

			if (table == null)
				throw new ArgumentNullException("table");

			this.handler = inner ?? new DefaultChannelMessageHandler(context, table);

			var builder = new ContainerBuilder();
			builder.RegisterInstance(context).ExternallyOwned(); // single instance for this and descendent scopes
			builder.Update(context.CurrentResolver.As<ILifetimeScope>().ComponentRegistry);
		}

		private readonly IMessageHandler<ChannelMessage> handler;
	}
}