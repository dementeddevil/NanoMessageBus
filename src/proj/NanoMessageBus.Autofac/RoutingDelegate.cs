namespace NanoMessageBus
{
	internal delegate int RoutingDelegate(IHandlerContext context, object message);

	internal delegate int RoutingDelegate<T>(IHandlerContext context, T message);
}