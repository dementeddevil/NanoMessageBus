namespace NanoMessageBus
{
	using System;
	using System.Linq;

	public class DefaultChannelMessageHandler : IMessageHandler<ChannelMessage>
	{
		public virtual void Handle(ChannelMessage message)
		{
			foreach (var logicalMessage in message.Messages.TakeWhile(x => this.context.ContinueHandling))
				this.routes.Route(this.context, logicalMessage);
		}

		public DefaultChannelMessageHandler(IHandlerContext context, IRoutingTable routes)
		{
			if (context == null)
				throw new ArgumentNullException("context");

			if (routes == null)
				throw new ArgumentNullException("routes");

			this.context = context;
			this.routes = routes;
		}

		private readonly IHandlerContext context;
		private readonly IRoutingTable routes;
	}
}