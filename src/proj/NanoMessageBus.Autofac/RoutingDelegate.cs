using System.Threading.Tasks;

namespace NanoMessageBus
{
	internal delegate Task<int> RoutingDelegate(IHandlerContext context, object message);

	internal delegate Task<int> RoutingDelegate<T>(IHandlerContext context, T message);
}