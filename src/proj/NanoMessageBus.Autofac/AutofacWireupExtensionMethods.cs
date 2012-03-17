namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;

	public static class AutofacWireupExtensionMethods
	{
		public static ICollection<Type> GetMessageHandlers(this IEnumerable<Assembly> assemblies)
		{
			var messageHandlers = new HashSet<Type>();
			foreach (var assembly in assemblies ?? new Assembly[0])
				messageHandlers.UnionWith(assembly.GetMessageHandlers());

			return messageHandlers;
		}
		public static ICollection<Type> GetMessageHandlers(this Assembly assembly)
		{
			if (assembly == null)
				throw new ArgumentNullException("assembly");

			ICollection<Type> handlers;
			if (MessageHandlers.TryGetValue(assembly, out handlers))
				return handlers;

			MessageHandlers[assembly] = handlers = new HashSet<Type>();
			foreach (var type in assembly.GetTypes().Where(x => x.GetMessageHandlerTypes().Count > 0))
				handlers.Add(type);

			return handlers;
		}

		public static IEnumerable<Type> GetMessageHandlerTypes(this Assembly assembly)
		{
			return assembly.GetMessageHandlers().SelectMany(x => x.GetMessageHandlerTypes());
		}
		public static ICollection<Type> GetMessageHandlerTypes(this Type messageHandler)
		{
			if (messageHandler == null)
				throw new ArgumentNullException("messageHandler");

			ICollection<Type> types;
			if (MessageHandlerTypes.TryGetValue(messageHandler, out types))
				return types;

			MessageHandlerTypes[messageHandler] = types = new HashSet<Type>();

			messageHandler.GetInterfaces()
				.Where(x => x.IsGenericType)
				.Where(x => x.GetGenericTypeDefinition() == typeof(IMessageHandler<>))
				.Select(x => x.GetGenericArguments().First())
				.ToList()
				.ForEach(types.Add);

			return types;
		}
		public static bool IsMessageHandler(this Type candidate)
		{
			if (candidate == null)
				throw new ArgumentNullException("candidate");

			return candidate.GetMessageHandlerTypes().Count > 0;
		}

		private static readonly IDictionary<Assembly, ICollection<Type>> MessageHandlers =
			new Dictionary<Assembly, ICollection<Type>>();
		private static readonly IDictionary<Type, ICollection<Type>> MessageHandlerTypes =
			new Dictionary<Type, ICollection<Type>>();
	}
}