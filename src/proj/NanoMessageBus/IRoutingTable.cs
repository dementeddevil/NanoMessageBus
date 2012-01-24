namespace NanoMessageBus
{
	using System;

	/// <summary>
	/// Provides the ability to route a given message to one or more registered handlers. In addition, multiple routing
	/// tables can be used by an IoC-managed application to have different routes depending upon which table is resolved
	/// for a given incoming message.
	/// </summary>
	/// <remarks>
	/// Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
	/// At the same time, routes should only be added to instances of this class during wireup rather than at runtime.
	/// </remarks>
	public interface IRoutingTable
	{
		/// <summary>
		/// Adds a route to the handler provided using the optional sequence specified. Adding the same handler multiple times
		/// will result in the most recent registration being used.
		/// </summary>
		/// <typeparam name="T">The type of message to be handled.</typeparam>
		/// <param name="handler">The handler into which the message will be routed.</param>
		/// <param name="sequence">The optional value which indicates priority over other handles for the same message.</param>
		/// <exception cref="ArgumentNullException"></exception>
		void Add<T>(IMessageHandler<T> handler, int sequence = int.MaxValue);

		/// <summary>
		/// Adds a route to the handler provided using the optional sequence specified. When the handler type is specified, adding
		/// the same handler multiple times will result in the most recent registration being used.
		/// </summary>
		/// <typeparam name="T">The type of message to be handled.</typeparam>
		/// <param name="callback">The callback used to resolve the handler instance into which the message will be routed.</param>
		/// <param name="sequence">The optional value which indicates priority over other handles for the same message.</param>
		/// <param name="handlerType">The optional type which indicates the type of handler to be returned by the handler callback.</param>
		/// <exception cref="ArgumentNullException"></exception>
		void Add<T>(Func<IHandlerContext, IMessageHandler<T>> callback, int sequence = int.MaxValue, Type handlerType = null);

		/// <summary>
		/// Routes the message provided to the associated message handlers.
		/// </summary>
		/// <param name="context">The context surrounding the handling of the channel message.</param>
		/// <param name="message">The logical message to be routed to the associated handlers.</param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <returns>The routes into which the message was routed.</returns>
		int Route(IHandlerContext context, object message);
	}
}