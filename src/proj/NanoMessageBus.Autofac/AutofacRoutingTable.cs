namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using Autofac;
	using Autofac.Core;
	using Logging;

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
			Log.Verbose("Attempting to route message of type '{0}' to registered handlers.", messageType);

			if (messageType == typeof(ChannelMessage))
				return this.RouteToChannelMessageHandler(context, message);

			return this.RouteToMessageHandlers(context, message, messageType) + DynamicRoute(context, message);
		}
		private int RouteToChannelMessageHandler(IHandlerContext context, object message)
		{
			this.channelMessageCallback(context).Handle(message as ChannelMessage);
			return 1;
		}
		private int RouteToMessageHandlers(IHandlerContext context, object message, Type messageType)
		{
			var handler = this.callbacks.TryGetValue(messageType);
			if (handler == null)
			{
				Log.Debug("No registered handlers for message of type '{0}'.", messageType);
				return 0;
			}

			handler(context, message);
			return 1;
		}
		private static int DynamicRoute<T>(IHandlerContext context, T message)
		{
			return TryResolve<T>(context)
				.TakeWhile(x => context.ContinueHandling)
				.Count(x => TryRoute(x, message));
		}
		private static IEnumerable<IMessageHandler<T>> TryResolve<T>(IDeliveryContext context)
		{
			var container = context.CurrentResolver.As<ILifetimeScope>();

			try
			{
				return container.Resolve<IEnumerable<IMessageHandler<T>>>();
			}
			catch (DependencyResolutionException e)
			{
				if (IsAutofacException(e))
					throw;

				throw e.InnerException;
			}
		}
		private static bool TryRoute<T>(IMessageHandler<T> route, T message)
		{
			try
			{
				route.Handle(message);
			}
			catch (AbortCurrentHandlerException e)
			{
				Log.Debug("Aborting executing of current handler of type '{0}' because of: {1}", route.GetType(), e.Message);
			}
			catch (DependencyResolutionException e)
			{
				if (IsAutofacException(e))
					throw;

				throw e.InnerException;
			}
			catch (Exception e)
			{
				Log.Debug("Message handler of type '{0}' threw an exception while handling message of type '{1}'.".FormatWith(route.GetType(), message.GetType()), e);
				throw;
			}

			return true;
		}
		private static bool IsAutofacException(DependencyResolutionException e)
		{
			return e.InnerException == null || e.InnerException.InnerException.GetType().FullName.StartsWith("Autofac");
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
				.PreserveExistingDefaults()
				.InstancePerLifetimeScope();
		}

		private static readonly ILog Log = LogFactory.Build(typeof(AutofacRoutingTable));
		private readonly IDictionary<Type, RoutingDelegate> callbacks = new Dictionary<Type, RoutingDelegate>();
		private Func<IHandlerContext, IMessageHandler<ChannelMessage>> channelMessageCallback;
		private static readonly MethodInfo DynamicRouteMethod =
			typeof(AutofacRoutingTable).GetMethod("DynamicRoute", BindingFlags.NonPublic | BindingFlags.Static);
	}
}