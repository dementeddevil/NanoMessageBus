namespace NanoMessageBus
{
	using System;
	using System.Reflection;

	internal static class AutofacRoutingTableExtensionMethods
	{
		public static RoutingDelegate AsCallback(this MethodInfo routeMethod, Type parameter)
		{
			var genericRouteMethod = routeMethod.MakeGenericMethod(parameter);
			var genericRouteDelegateType = typeof(RoutingDelegate<>).MakeGenericType(parameter);
			var callback = Delegate.CreateDelegate(genericRouteDelegateType, genericRouteMethod);
			var genericWrapMethod = WrapCallbackMethod.MakeGenericMethod(parameter);
			return (RoutingDelegate)genericWrapMethod.Invoke(null, new object[] { callback });
		}
		private static RoutingDelegate WrapCallback<T>(RoutingDelegate<T> callback)
		{
			return (context, message) => callback(context, (T)message);
		}

		private static readonly MethodInfo WrapCallbackMethod =
			typeof(AutofacRoutingTableExtensionMethods).GetMethod("WrapCallback", BindingFlags.NonPublic | BindingFlags.Static);
	}
}