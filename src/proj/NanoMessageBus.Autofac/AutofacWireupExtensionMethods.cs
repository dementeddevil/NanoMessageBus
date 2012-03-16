namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;

	public static class AutofacWireupExtensionMethods
	{
		public static IEnumerable<Type> GetHandledTypes(this IEnumerable<Assembly> assemblies)
		{
			return (assemblies ?? new Assembly[0])
				.SelectMany(assembly => assembly.GetHandledTypes())
				.Distinct()
				.ToArray();
		}
		public static IEnumerable<Type> GetHandledTypes(this Assembly assembly)
		{
			return assembly.GetTypes()
				.SelectMany(GetHandledTypes)
				.Distinct()
				.ToArray();
		}
		public static IEnumerable<Type> GetHandledTypes(this Type type)
		{
			return type.GetInterfaces()
				.Where(x => x.IsGenericType)
				.Where(x => x.GetGenericTypeDefinition() == typeof(IMessageHandler<>))
				.Select(x => x.GetGenericArguments().First());
		}

		internal static RoutingDelegate AsCallback(this MethodInfo routeMethod, Type parameter)
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
			typeof(AutofacWireupExtensionMethods).GetMethod("WrapCallback", BindingFlags.NonPublic | BindingFlags.Static);
	}
}