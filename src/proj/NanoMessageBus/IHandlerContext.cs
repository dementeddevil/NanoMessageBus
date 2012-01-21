namespace NanoMessageBus
{
	using System.Collections.Generic;

	/// <summary>
	/// Represents the attempt to delivery a set of logical messages to the associated message handlers.
	/// </summary>
	/// <remarks>
	/// Instances of this class are single threaded and should not be shared between threads.
	/// </remarks>
	public interface IHandlerContext
	{
		/// <summary>
		/// Gets all context associated with the attempted delivery of the channel message.
		/// </summary>
		IDeliveryContext Delivery { get; }

		/// <summary>
		/// Gets a value indicating whether or not processing of the given channel message should continue.
		/// </summary>
		bool ContinueHandling { get; }

		/// <summary>
		/// Stops handling the current channel message and consumes the message.
		/// </summary>
		void DropMessage();

		/// <summary>
		/// Stops handling the channel message and re-enqueues it for later delivery.
		/// </summary>
		void DeferMessage();

		/// <summary>
		/// Sends one or more logical messages to the configured endpoints as a single channel message.
		/// </summary>
		/// <param name="messages">The message(s) to be sent.</param>
		void Send(params object[] messages);

		/// <summary>
		/// Publishes one or more logical messages to any registered subscribers as a single channel message.
		/// </summary>
		/// <param name="messages">The message(s) to be published.</param>
		void Publish(params object[] messages);

		/// <summary>
		/// Returns one or more logical messages back to the point of origin as a single channel message.
		/// </summary>
		/// <param name="messages">The message(s) to be sent back to the point of origin.</param>
		void Reply(params object[] messages);

		// TODO: outbound headers *per* message? (instead of collective outgoing headers)
		// TODO: how to indicate correlation ID on a message?
		// TODO: how do outbound messages determine the correct ReturnAddress?

		/// <summary>
		/// Gets a set of headers to be appended any outgoing channel message envelope.
		/// </summary>
		IDictionary<string, string> OutgoingHeaders { get; }
	}
}