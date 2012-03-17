﻿namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using Autofac;

	public class AutofacRoutingTable : IRoutingTable
	{
		public virtual void Add<T>(IMessageHandler<T> handler, int sequence = 2147483647)
		{
			throw new NotSupportedException("No registration required.");
		}
		public virtual void Add<T>(Func<IHandlerContext, IMessageHandler<T>> callback, int sequence = 2147483647, Type handlerType = null)
		{
			if (typeof(T) != typeof(ChannelMessage))
				throw new NotSupportedException("No registration required.");

			this.channelMessageCallback = x => (IMessageHandler<ChannelMessage>)callback(x);
		}

		public virtual int Route(IHandlerContext context, object message)
		{
			var messageType = message.GetType();
			if (messageType == typeof(ChannelMessage))
				return this.RouteToChannelMessageHandler(context, message);

			var routes = this.callbacks[messageType](context, message);
			if (routes == 0)
				return DynamicRoute(context, message);

			return routes;
		}
		private int RouteToChannelMessageHandler(IHandlerContext context, object message)
		{
			this.channelMessageCallback(context).Handle(message as ChannelMessage);
			return 1;
		}
		private static int DynamicRoute<T>(IHandlerContext context, T message)
		{
			var container = context.CurrentResolver.As<ILifetimeScope>();
			var routes = container.Resolve<IEnumerable<IMessageHandler<T>>>();

			return routes
				.TakeWhile(x => context.ContinueHandling)
				.Count(x => { x.Handle(message); return true; });
		}

		public static void AddHandlerInterfaceType(Type handlerType)
		{
			if (handlerType == null)
				throw new ArgumentNullException("handlerType");

			if (!handlerType.IsInterface)
				throw new ArgumentException("Type must be an interface");

			if (!handlerType.IsGenericType)
				throw new ArgumentException("Type must be generic");

			MessageHandlerInterfaces.Add(handlerType);
		}

		static AutofacRoutingTable()
		{
			MessageHandlerInterfaces.Add(typeof(IMessageHandler<>));
		}
		public AutofacRoutingTable(params Assembly[] messageHandlerAssemblies)
			: this(null, messageHandlerAssemblies)
		{
		}
		public AutofacRoutingTable(ContainerBuilder builder, params Assembly[] messageHandlerAssemblies)
		{
			var messageHandlers = messageHandlerAssemblies.GetMessageHandlers();
			foreach (var handledType in messageHandlers.SelectMany(x => x.GetMessageHandlerTypes()))
				this.callbacks[handledType] = DynamicRouteMethod.AsCallback(handledType);

			if (builder == null)
				return;

			builder
				.RegisterInstance(this)
				.As<IRoutingTable>()
				.SingleInstance();

			builder
				.RegisterAssemblyTypes(messageHandlerAssemblies)
				.Where(messageHandlers.Contains)
				.AsImplementedInterfaces()
				.InstancePerLifetimeScope();
		}

		internal static readonly ICollection<Type> MessageHandlerInterfaces = new HashSet<Type>();
		private readonly IDictionary<Type, RoutingDelegate> callbacks = new Dictionary<Type, RoutingDelegate>();
		private Func<IHandlerContext, IMessageHandler<ChannelMessage>> channelMessageCallback;
		private static readonly MethodInfo DynamicRouteMethod =
			typeof(AutofacRoutingTable).GetMethod("DynamicRoute", BindingFlags.NonPublic | BindingFlags.Static);
	}
}