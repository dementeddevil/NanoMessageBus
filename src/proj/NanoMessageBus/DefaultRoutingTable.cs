using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NanoMessageBus.Logging;

namespace NanoMessageBus
{
    public class DefaultRoutingTable : IRoutingTable
    {
        public virtual void Add<T>(IMessageHandler<T> handler, int sequence = int.MaxValue)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            Log.Debug("Registering handle of type '{0}' for messages of type '{1}' at sequence {2}.",
                handler.GetType(), typeof(T), sequence);

            this.Add<T>(new SimpleHandler<T>(handler, sequence), handler.GetType());
        }

        public virtual void Add<T>(Func<IHandlerContext, IMessageHandler<T>> callback, int sequence = int.MaxValue, Type handlerType = null)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            Log.Debug("Registering callback of type '{0}' for messages of type '{1}' at sequence {2}.",
                handlerType, typeof(T), sequence);

            this.Add<T>(new CallbackHandler<T>(callback, sequence, handlerType), handlerType);
        }

        public virtual async Task<int> Route(IHandlerContext context, object message)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (message == null)
                throw new ArgumentNullException(nameof(message));

            Log.Verbose("Attempting to route message of type '{0}' to registered handlers.", message.GetType());

            List<ISequencedHandler> routes;
            if (!this._registeredRoutes.TryGetValue(message.GetType(), out routes))
            {
                Log.Debug("No registered handlers for message of type '{0}'.", message.GetType());
                return 0;
            }

            // FUTURE: route to handlers for message base classes and interfaces all the way back to System.Object
            int count = 0;
            foreach (var route in routes)
            {
                if (!context.ContinueHandling)
                {
                    break;
                }

                await TryRoute(route, context, message).ConfigureAwait(false);
                ++count;
            }
            return count;
        }

        private void Add<T>(ISequencedHandler handler, Type handlerType)
        {
            List<ISequencedHandler> routes;
            if (!this._registeredRoutes.TryGetValue(typeof(T), out routes))
                routes = new List<ISequencedHandler>();

            if (this._registeredHandlers.Contains(handlerType))
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
                this._registeredHandlers.Add(handlerType);

            this._registeredRoutes[typeof(T)] = routes.OrderBy(x => x.Sequence).ToList();
        }

        private static Task<bool> TryRoute(ISequencedHandler route, IHandlerContext context, object message)
        {
            try
            {
                return route.Handle(context, message);
            }
            catch (AbortCurrentHandlerException e)
            {
                Log.Debug("Aborting executing of current handler of type '{0}' because of: {1}",
                    route.HandlerType, e.Message);

                return Task.FromResult(true);
            }
        }

        private static readonly ILog Log = LogFactory.Build(typeof(DefaultRoutingTable));
        private readonly ICollection<Type> _registeredHandlers = new HashSet<Type>();
        private readonly IDictionary<Type, List<ISequencedHandler>> _registeredRoutes =
            new Dictionary<Type, List<ISequencedHandler>>();

        private interface ISequencedHandler
        {
            int Sequence { get; }

            Type HandlerType { get; }

            Task<bool> Handle(IHandlerContext context, object message);
        }

        private class SimpleHandler<T> : ISequencedHandler
        {
            private readonly IMessageHandler<T> _handler;

            public SimpleHandler(IMessageHandler<T> handler, int sequence)
            {
                _handler = handler;
                HandlerType = handler.GetType();
                Sequence = sequence;
            }

            public int Sequence { get; private set; }

            public Type HandlerType { get; private set; }

            public async Task<bool> Handle(IHandlerContext context, object message)
            {
                var messageType = message.GetType();
                var handlerType = _handler.GetType();
                Log.Verbose($"Pushing message of type '{messageType}' into handler of type '{handlerType}'.");

                try
                {
                    await _handler.HandleAsync((T)message).ConfigureAwait(false);
                    return true;
                }
                catch (AbortCurrentHandlerException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    Log.Debug($"Message handler of type '{handlerType}' threw an exception while handling message of type '{messageType}'.", e);
                    throw;
                }
            }
        }

        private class CallbackHandler<T> : ISequencedHandler
        {
            private readonly Func<IHandlerContext, IMessageHandler<T>> _callback;

            public CallbackHandler(Func<IHandlerContext, IMessageHandler<T>> callback, int sequence, Type handlerType)
            {
                this._callback = callback;
                this.HandlerType = handlerType;
                this.Sequence = sequence;
            }

            public int Sequence { get; private set; }

            public Type HandlerType { get; private set; }

            public async Task<bool> Handle(IHandlerContext context, object message)
            {
                var handler = _callback(context);
                if (handler == null)
                {
                    Log.Debug("Unable to resolve a handler from the callback registered for handler of type '{0}' and message of type '{1}'.",
                        this.HandlerType, typeof(T));
                    return false;
                }

                var messageType = message.GetType();
                var handlerType = handler.GetType();
                Log.Verbose($"Pushing message of type '{messageType}' into handler of type '{handlerType}'.");

                try
                {
                    await handler.HandleAsync((T)message);
                    return true;
                }
                catch (AbortCurrentHandlerException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    Log.Debug($"Message handler of type '{handlerType}' threw an exception while handling message of type '{messageType}'.", e);
                    throw;
                }
            }
        }
    }
}