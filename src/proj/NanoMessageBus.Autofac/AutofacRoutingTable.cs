using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using NanoMessageBus.Logging;

namespace NanoMessageBus
{

	public class AutofacRoutingTable : IRoutingTable
	{
        private static readonly ILog Log = LogFactory.Build(typeof(AutofacRoutingTable));
        private static readonly MethodInfo DynamicRouteMethod =
            typeof(AutofacRoutingTable).GetMethod("DynamicRoute", BindingFlags.NonPublic | BindingFlags.Static);

        private readonly IDictionary<Type, RoutingDelegate> _callbacks = new Dictionary<Type, RoutingDelegate>();
        private Func<IHandlerContext, IMessageHandler<ChannelMessage>> _channelMessageCallback;

        public virtual void Add<T>(IMessageHandler<T> handler, int sequence = int.MaxValue)
		{
			throw new NotSupportedException("No registration required.");
		}

        public virtual void Add<T>(Func<IHandlerContext, IMessageHandler<T>> callback, int sequence = int.MaxValue, Type handlerType = null)
		{
			if (typeof(T) != typeof(ChannelMessage))
				throw new NotSupportedException("No registration required.");

			_channelMessageCallback = x => (IMessageHandler<ChannelMessage>)callback(x);
		}

        public virtual async Task<int> Route(IHandlerContext context, object message)
		{
			var messageType = message.GetType();
			Log.Verbose("Attempting to route message of type '{0}' to registered handlers.", messageType);

            if (messageType == typeof(ChannelMessage))
            {
                return await RouteToChannelMessageHandler(context, message).ConfigureAwait(false);
            }

            int count = await RouteToMessageHandlers(context, message, messageType).ConfigureAwait(false);
            count += await DynamicRoute(context, message).ConfigureAwait(false);
            return count;
		}

		private async Task<int> RouteToChannelMessageHandler(IHandlerContext context, object message)
		{
			await _channelMessageCallback(context).HandleAsync(message as ChannelMessage).ConfigureAwait(false);
			return 1;
		}

		private Task<int> RouteToMessageHandlers(IHandlerContext context, object message, Type messageType)
		{
			var handler = _callbacks.TryGetValue(messageType);
			if (handler == null)
			{
				Log.Debug($"No registered handlers for message of type '{messageType}'.");
				return Task.FromResult(0);
			}

			handler(context, message);
			return Task.FromResult(1);
		}

		private static async Task<int> DynamicRoute<T>(IHandlerContext context, T message)
		{
		    int count = 0;

			var routes = TryResolve<T>(context);
		    foreach (var route in routes)
		    {
		        if (!context.ContinueHandling)
		        {
		            break;
		        }

		        if (await TryRoute(route, message).ConfigureAwait(false))
		        {
		            ++count;
		        }
		    }

		    return count;
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

		private static async Task<bool> TryRoute<T>(IMessageHandler<T> route, T message)
		{
			try
			{
				await route.HandleAsync(message).ConfigureAwait(false);
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
			var inner = e.InnerException;
			return inner == null || inner.GetType().FullName.StartsWith("Autofac");
		}

		public AutofacRoutingTable(params Assembly[] messageHandlerAssemblies)
			: this(null, messageHandlerAssemblies)
		{
		}

		public AutofacRoutingTable(ContainerBuilder builder, params Assembly[] messageHandlerAssemblies)
		{
			var messageHandlers = messageHandlerAssemblies.GetMessageHandlers();
		    foreach (var handledType in messageHandlers.SelectMany(x => x.GetMessageHandlerTypes()))
		    {
		        _callbacks[handledType] = DynamicRouteMethod.AsCallback(handledType);
		    }

		    if (builder == null)
		    {
		        return;
		    }

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
	}
}