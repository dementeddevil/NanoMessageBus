namespace NanoMessageBus
{
	using System.Collections.Generic;

	/// <summary>
	/// Represents the attempt to delivery a set of logical messages to the associated message handlers.
	/// </summary>
	public interface IHandlerContext
	{
		/// <summary>
		/// Gets a value indicating whether or not processing of the given physical message should continue.
		/// </summary>
		bool ContinueHandling { get; }

		/// <summary>
		/// Gets all contexted associated with the attempted delivery of the message.
		/// </summary>
		IDeliveryContext Delivery { get; }

		/// <summary>
		/// Stops handling the current physical message and consumes the message.
		/// </summary>
		void DropMessage();

		/// <summary>
		/// Stops handling the current physical message and re-enqueues the message for later delivery.
		/// </summary>
		void DeferMessage();

		/// <summary>
		/// Sends one or more logical messages to the configured endpoints.
		/// </summary>
		/// <param name="messages">The message(s) to be sent.</param>
		void Send(params object[] messages);

		/// <summary>
		/// Publishes one or more logical messages to any registered subscribers.
		/// </summary>
		/// <param name="messages">The message(s) to be published.</param>
		void Publish(params object[] messages);

		/// <summary>
		/// Returns one or more logical messages back to the point of origin.
		/// </summary>
		/// <param name="messages">The message(s) to be sent back to the point of origin.</param>
		void Reply(params object[] messages);

		/// <summary>
		/// Gets a set of headers to be appended any outgoing messages.
		/// </summary>
		IDictionary<string, string> OutgoingHeaders { get; }
	}
}