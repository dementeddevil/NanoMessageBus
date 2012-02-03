namespace NanoMessageBus
{
	using System;
	using Autofac;

	public partial class AutofacRoutingTable : IRoutingTable
	{
		public virtual void Add<T>(IMessageHandler<T> handler, int sequence = 2147483647)
		{
			// TODO
		}
		public virtual void Add<T>(Func<IHandlerContext, IMessageHandler<T>> callback, int sequence = 2147483647, Type handlerType = null)
		{
			// TODO
		}

		public virtual int Route(IHandlerContext context, object message)
		{
			var container = context.CurrentResolver.As<ILifetimeScope>();
			var componentContext = container.Resolve<IComponentContext>();

			// TODO
			return 0;
		}
	}
}