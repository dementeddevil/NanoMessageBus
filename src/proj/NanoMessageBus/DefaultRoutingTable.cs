namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class DefaultRoutingTable : IRoutingTable
	{
		public virtual void Add<T>(IMessageHandler<T> handler, int sequence = int.MaxValue)
		{
			if (handler == null)
				throw new ArgumentNullException("handler");

			if (this.registeredHandlers.Contains(handler.GetType()))
				return; // TODO: overwrite existing registration with latest one

			this.registeredHandlers.Add(handler.GetType());

			IList<ISequencedHandler> routes;
			if (!this.registeredRoutes.TryGetValue(typeof(T), out routes))
				routes = new List<ISequencedHandler>();

			routes.Add(new SimpleHandler<T>(handler, sequence));
			this.registeredRoutes[typeof(T)] = routes.OrderBy(x => x.Sequence).ToList();
		}
		public virtual void Add<T>(
			Func<IHandlerContext, IMessageHandler<T>> callback, int sequence = int.MaxValue, Type handlerType = null)
		{
			if (callback == null)
				throw new ArgumentNullException("callback");

			if (handlerType != null && this.registeredHandlers.Contains(handlerType))
				return; // TODO: overwrite existing registration with the latest one
			if (handlerType != null)
				this.registeredHandlers.Add(handlerType);

			IList<ISequencedHandler> routes;
			if (!this.registeredRoutes.TryGetValue(typeof(T), out routes))
				routes = new List<ISequencedHandler>();

			routes.Add(new CallbackHandler<T>(callback, sequence));
			this.registeredRoutes[typeof(T)] = routes.OrderBy(x => x.Sequence).ToList();
		}
		public virtual void Route(IHandlerContext context, object message)
		{
			if (context == null)
				throw new ArgumentNullException("context");

			if (message == null)
				throw new ArgumentNullException("message");

			IList<ISequencedHandler> routes;
			if (!this.registeredRoutes.TryGetValue(message.GetType(), out routes))
				return;

			foreach (var route in routes.TakeWhile(x => context.ContinueHandling))
				route.Handle(context, message);
		}

		private readonly ICollection<Type> registeredHandlers = new HashSet<Type>();
		private readonly IDictionary<Type, IList<ISequencedHandler>> registeredRoutes =
			new Dictionary<Type, IList<ISequencedHandler>>();

		private interface ISequencedHandler
		{
			int Sequence { get; }
			void Handle(IHandlerContext context, object message);
		}
		private class SimpleHandler<T> : ISequencedHandler
		{
			public int Sequence { get; private set; }
			public void Handle(IHandlerContext context, object message)
			{
				this.handler.Handle((T)message);
			}
			public SimpleHandler(IMessageHandler<T> handler, int sequence)
			{
				this.handler = handler;
				this.Sequence = sequence;
			}
			private readonly IMessageHandler<T> handler;
		}
		private class CallbackHandler<T> : ISequencedHandler
		{
			public int Sequence { get; private set; }
			public void Handle(IHandlerContext context, object message)
			{
				var handler = this.callback(context);
				if (handler != null)
					handler.Handle((T)message);
			}
			public CallbackHandler(Func<IHandlerContext, IMessageHandler<T>> callback, int sequence)
			{
				this.callback = callback;
				this.Sequence = sequence;
			}
			private readonly Func<IHandlerContext, IMessageHandler<T>> callback;
		}
	}
}