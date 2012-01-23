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

			this.Add<T>(new SimpleHandler<T>(handler, sequence), handler.GetType());
		}
		public virtual void Add<T>(Func<IHandlerContext, IMessageHandler<T>> callback, int sequence = int.MaxValue, Type handlerType = null)
		{
			if (callback == null)
				throw new ArgumentNullException("callback");

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
					routes[index] = handler;
			}
			else
				routes.Add(handler);

			if (handlerType != null)
				this.registeredHandlers.Add(handlerType);

			this.registeredRoutes[typeof(T)] = routes.OrderBy(x => x.Sequence).ToList();
		}

		public virtual void Route(IHandlerContext context, object message)
		{
			if (context == null)
				throw new ArgumentNullException("context");

			if (message == null)
				throw new ArgumentNullException("message");

			List<ISequencedHandler> routes;
			if (!this.registeredRoutes.TryGetValue(message.GetType(), out routes))
				routes = new List<ISequencedHandler>();

			// FUTURE: route to handlers for message base classes and interfaces all the way back to System.Object
			if (!RouteToHandlers(routes, context, message))
				this.ForwardToDeadLetters(context.Delivery, message);
		}
		private static bool RouteToHandlers(IEnumerable<ISequencedHandler> routes, IHandlerContext context, object message)
		{
			var handled = false;
			foreach (var route in routes.TakeWhile(x => context.ContinueHandling))
				if (route.Handle(context, message) && !handled)
					handled = true;

			return handled;
		}
		protected virtual void ForwardToDeadLetters(IDeliveryContext context, object message)
		{
			var channelMessage = this.BuildMessage(context, message);
			context.Send(new ChannelEnvelope(channelMessage, new[] { ChannelEnvelope.DeadLetterAddress }));
		}
		protected virtual ChannelMessage BuildMessage(IDeliveryContext context, object message)
		{
			var msg = context.CurrentMessage;
			if (message == msg)
				return message as ChannelMessage;

			return new ChannelMessage(Guid.NewGuid(), msg.CorrelationId, msg.ReturnAddress, msg.Headers, new[] { message });
		}

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
				this.handler.Handle((T)message);
				return true;
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
					return false;

				handler.Handle((T)message);
				return true;
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