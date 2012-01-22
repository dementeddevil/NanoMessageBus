namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Provides the ability to determine the set of recipients (by address) for a given type of message.
	/// </summary>
	/// <remarks>
	/// Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
	/// </remarks>
	public interface IDispatchTable
	{
		/// <summary>
		/// Gets the set of recipients of the message type specified.
		/// </summary>
		/// <param name="messageType">The type of message to use when determining the set of subscribers.</param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <returns>The set addresses for all registered subscribers for the given message type, if any.</returns>
		ICollection<Uri> this[Type messageType] { get; }

		/// <summary>
		/// Adds the subscriber to the set of subscribers for the specified message type.
		/// </summary>
		/// <param name="subscriber">The address of the subscriber to be added.</param>
		/// <param name="messageType">The type of message in which the subscriber is interested.</param>
		/// <param name="expiration">The point in time at which the subscription for the given type will expire.</param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <remarks>
		/// Subscribers are used when "publishing" a message. Multiple calls will only add the subscriber once with
		/// the latest expiration value provided being used.
		/// </remarks>
		void AddSubscriber(Uri subscriber, Type messageType, DateTime expiration);

		/// <summary>
		/// Adds a recipient to the set of recipients for the specified message type.
		/// </summary>
		/// <param name="recipient">The address of the recipient to be added.</param>
		/// <param name="messageType">The type of message in which the subscriber is interested.</param>
		/// <remarks>
		/// Recipients are used when "sending" a message.
		/// </remarks>
		void AddRecipient(Uri recipient, Type messageType);

		/// <summary>
		/// Removes the subscriber or recipient from the set of subscribers for the specified message type.
		/// </summary>
		/// <param name="address">The address of the subscriber or recipient to be removed.</param>
		/// <param name="messageType">The type of message in which the subscriber or recipient is no longer interested.</param>
		/// <exception cref="ArgumentNullException"></exception>
		void Remove(Uri address, Type messageType);
	}
}