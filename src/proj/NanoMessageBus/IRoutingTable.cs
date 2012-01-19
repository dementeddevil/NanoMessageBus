namespace NanoMessageBus
{
	using System;

	/// <summary>
	/// Provides the ability to route a given message to one or more registered.
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
		/// <exception cref="IndexOutOfRangeException"></exception>
		void Add<T>(IMessageHandler<T> handler, int sequence = 0);

		/// <summary>
		/// Adds a route to the handler provided using the optional sequence specified. Adding the same handler multiple times
		/// will result in the most recent registration being used.
		/// </summary>
		/// <typeparam name="T">The type of message to be handled.</typeparam>
		/// <param name="handler">The callback used to resolve the handler into which the message will be routed.</param>
		/// <param name="sequence">The optional value which indicates priority over other handles for the same message.</param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="IndexOutOfRangeException"></exception>
		void Add<T>(Func<IMessageHandler<T>> handler, int sequence = 0);

		/// <summary>
		/// Adds a route to the handler provided using the optional sequence specified. Adding the same handler multiple times
		/// will result in the most recent registration being used.
		/// </summary>
		/// <typeparam name="T">The type of message to be handled.</typeparam>
		/// <param name="handler">The callback used to resolve the handler into which the message will be routed.</param>
		/// <param name="sequence">The optional value which indicates priority over other handles for the same message.</param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="IndexOutOfRangeException"></exception>
		void Add<T>(Func<IHandlerContext, IMessageHandler<T>> handler, int sequence = 0);

		/// <summary>
		/// Routes the message provided to the associated message handlers.
		/// </summary>
		/// <param name="context">The context surrounding the handling of the channel message.</param>
		/// <param name="message">The logical message to be routed to the associated handlers.</param>
		/// <exception cref="ArgumentNullException"></exception>
		void Route(IHandlerContext context, object message);
	}
}