namespace NanoMessageBus
{
	using System;
	using System.Collections;
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
			if (typeof(T) != ChannelMessageType)
				throw new NotSupportedException("No registration required.");

			this.channelMessageCallback = x => (IMessageHandler<ChannelMessage>)callback(x);
		}

		public virtual int Route(IHandlerContext context, object message)
		{
			// TODO: perhaps we make a Route<T> method call which then facilitates generics all the way down
			// and eliminates a lot of the games here.

			var messageType = message.GetType();
			if (messageType == ChannelMessageType && this.channelMessageCallback != null)
			{
				this.channelMessageCallback(context).Handle(message as ChannelMessage);
				return 1;
			}

			var autofac = context.CurrentResolver.As<ILifetimeScope>().Resolve<IComponentContext>();
			var handlers = autofac.Resolve(this.messageTypes[messageType]) as IEnumerable;
			return (handlers ?? new object[0]).Cast<object>().Count(handler =>
			{
				this.handlerDelegates[handler.GetType()][message.GetType()].DynamicInvoke(handler, message);
				return true;
			});
		}

		public AutofacRoutingTable(params Assembly[] messageHandlers)
			: this((IEnumerable<Assembly>)messageHandlers)
		{
		}
		public AutofacRoutingTable(IEnumerable<Assembly> messageHandlers)
		{
			messageHandlers = messageHandlers ?? new Assembly[0];
			this.RegisterMessageTypes(messageHandlers);
		}
		private void RegisterMessageTypes(IEnumerable<Assembly> messageHandlers)
		{
			foreach (var handler in messageHandlers.SelectMany(x => x.GetTypes()))
			{
				foreach (var parameter in GetTypeParameters(handler))
				{
					this.RegisterMessageHandlerType(handler, parameter);
					this.RegisterMessageType(parameter);
				}
			}
		}
		private void RegisterMessageHandlerType(Type handler, Type parameter)
		{
			IDictionary<Type, Delegate> delegates;
			if (!this.handlerDelegates.TryGetValue(handler, out delegates))
				this.handlerDelegates[handler] = delegates = new Dictionary<Type, Delegate>();

			var generic = typeof(Action<,>).MakeGenericType(handler, parameter);
			var method = handler.GetMethod(HandleMethod, new[] { parameter });
			delegates[parameter] = Delegate.CreateDelegate(generic, method);
		}
		private void RegisterMessageType(Type parameter)
		{
			if (this.messageTypes.ContainsKey(parameter))
				return;

			var handlerType = MessageHandlerInterfaceType.MakeGenericType(parameter);
			handlerType = EnumerableType.MakeGenericType(handlerType);
			this.messageTypes[parameter] = handlerType;
		}
		private static IEnumerable<Type> GetTypeParameters(Type type)
		{
			return type.GetInterfaces()
				.Where(x => x.IsGenericType)
				.Where(x => x.GetGenericTypeDefinition() == MessageHandlerInterfaceType)
				.Select(x => x.GetGenericArguments().First())
				.ToArray();
		}

		private static readonly Type ChannelMessageType = typeof(ChannelMessage);
		private static readonly Type EnumerableType = typeof(IEnumerable<>);
		private static readonly Type MessageHandlerInterfaceType = typeof(IMessageHandler<>);
		private static readonly string HandleMethod = MessageHandlerInterfaceType.GetMethods()[0].Name;
		private readonly IDictionary<Type, Type> messageTypes = new Dictionary<Type, Type>();
		private readonly IDictionary<Type, IDictionary<Type, Delegate>> handlerDelegates =
			new Dictionary<Type, IDictionary<Type, Delegate>>();
		private Func<IHandlerContext, IMessageHandler<ChannelMessage>> channelMessageCallback;
	}
}