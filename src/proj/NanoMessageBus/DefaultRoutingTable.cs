namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Logging;

	public class DefaultRoutingTable : IRoutingTable
	{
		public virtual void Add<T>(IMessageHandler<T> handler, int sequence = int.MaxValue)
		{
			if (handler == null)
				throw new ArgumentNullException("handler");

			Log.Debug("Registering handle of type '{0}' for messages of type '{1}' at sequence {2}.",
				handler.GetType(), typeof(T), sequence);

			this.Add<T>(new SimpleHandler<T>(handler, sequence), handler.GetType());
		}
		public virtual void Add<T>(Func<IHandlerContext, IMessageHandler<T>> callback, int sequence = int.MaxValue, Type handlerType = null)
		{
			if (callback == null)
				throw new ArgumentNullException("callback");

			Log.Debug("Registering callback of type '{0}' for messages of type '{1}' at sequence {2}.",
				handlerType, typeof(T), sequence);

			this.Add<T>(new CallbackHandler<T>(callback, sequence, handlerType), handlerType);
		}
		private void Add<T>(ISequencedHandler handler, Type handlerType)
		{
			List<ISequencedHandler> routes;
			if (!this.registeredRoutes.TryGetValue(typeof(T), out routes))
				routes = new List<ISequencedHandler>();

			if (this.registeredHandlers.Contains(handlerType))
			{
				var index = routes.FindIndex(x => x.HandlerType == handlerType);
				if (index >= 0)
				{
					Log.Debug("Handler of type '{0}' already registered, replacing previously registered handler.", handlerType);
					routes[index] = handler;
				}
			}
			else
				routes.Add(handler);

			if (handlerType != null)
				this.registeredHandlers.Add(handlerType);

			this.registeredRoutes[typeof(T)] = routes.OrderBy(x => x.Sequence).ToList();
		}

		public virtual int Route(IHandlerContext context, object message)
		{
			if (context == null)
				throw new ArgumentNullException("context");

			if (message == null)
				throw new ArgumentNullException("message");

			Log.Verbose("Attempting to route message of type '{0}' to registered handlers.", message.GetType());

			List<ISequencedHandler> routes;
			if (!this.registeredRoutes.TryGetValue(message.GetType(), out routes))
			{
				Log.Debug("No registered handlers for message of type '{0}'.", message.GetType());
				return 0;
			}

			// FUTURE: route to handlers for message base classes and interfaces all the way back to System.Object
			return routes
				.TakeWhile(x => context.ContinueHandling)
				.Count(route => TryRoute(route, context, message));
		}
		private static bool TryRoute(ISequencedHandler route, IHandlerContext context, object message)
		{
			try
			{
				return route.Handle(context, message);
			}
			catch (AbortCurrentHandlerException e)
			{
				Log.Debug("Aborting executing of current handler of type '{0}' because of: {1}",
					route.HandlerType, e.Message);

				return true;
			}
		}

		private static readonly ILog Log = LogFactory.Build(typeof(DefaultRoutingTable));
		private readonly ICollection<Type> registeredHandlers = new HashSet<Type>();
		private readonly IDictionary<Type, List<ISequencedHandler>> registeredRoutes =
			new Dictionary<Type, List<ISequencedHandler>>();

		private interface ISequencedHandler
		{
			int Sequence { get; }
			Type HandlerType { get; }
			bool Handle(IHandlerContext context, object message);
		}
		private class SimpleHandler<T> : ISequencedHandler
		{
			public int Sequence { get; private set; }
			public Type HandlerType { get; private set; }
			public bool Handle(IHandlerContext context, object message)
			{
				Log.Verbose("Pushing message of type '{0}' into handler of type '{1}'.", typeof(T), this.HandlerType);

				try
				{
					this.handler.Handle((T)message);
					return true;
				}
				catch (Exception e)
				{
					Log.Error("Message handler of type '{0}' threw an exception of type '{1}' when handling message of type '{2}': {3}",
						this.HandlerType, e.GetType(), typeof(T), e.Message);

					throw;
				}
			}
			public SimpleHandler(IMessageHandler<T> handler, int sequence)
			{
				this.handler = handler;
				this.HandlerType = handler.GetType();
				this.Sequence = sequence;
			}
			private readonly IMessageHandler<T> handler;
		}
		private class CallbackHandler<T> : ISequencedHandler
		{
			public int Sequence { get; private set; }
			public Type HandlerType { get; private set; }
			public bool Handle(IHandlerContext context, object message)
			{
				var handler = this.callback(context);
				if (handler == null)
				{
					Log.Debug("Unable to resolve a handler from the callback registered for handler of type '{0}' and message of type '{1}'.", this.HandlerType, typeof(T));
					return false;
				}

				Log.Verbose("Pushing message of type '{0}' into handler of type '{1}'.", typeof(T), this.HandlerType);

				try
				{
					handler.Handle((T)message);
					return true;
				}
				catch (Exception e)
				{
					Log.Error("Message handler of type '{0}' threw an exception of type '{1}' when handling message of type '{2}': {3}",
						this.HandlerType, e.GetType(), typeof(T), e.Message);

					throw;
				}
			}
			public CallbackHandler(Func<IHandlerContext, IMessageHandler<T>> callback, int sequence, Type handlerType)
			{
				this.callback = callback;
				this.HandlerType = handlerType;
				this.Sequence = sequence;
			}
			private readonly Func<IHandlerContext, IMessageHandler<T>> callback;
		}
	}
}