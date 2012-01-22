namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Provides the ability to determine the set of subscribers (by address) for a given type of message.
	/// </summary>
	/// <remarks>
	/// Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
	/// </remarks>
	public interface ISubscriberTable
	{
		/// <summary>
		/// Gets the set of subscribers of the message type specified.
		/// </summary>
		/// <param name="messageType">The type of message to use when determining the set of subscribers.</param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <returns>The set addresses for all registered subscribers for the given message type, if any.</returns>
		ICollection<Uri> this[Type messageType] { get; }

		/// <summary>
		/// Adds the subscriber to the set of subscribers of the specified message type.
		/// </summary>
		/// <param name="subscriber">The address of the subscriber to be added.</param>
		/// <param name="messageType">The type of message in which the subscriber is interested.</param>
		/// <param name="expiration">The point in time at which the subscription for the given type will expire.</param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <remarks>
		/// Multiple calls will only add the subscriber once with the latest expiration value provided being used.
		/// </remarks>
		void Add(Uri subscriber, Type messageType, DateTime expiration);

		/// <summary>
		/// Removes the subscriber from the set of subscribers for the specified message type.
		/// </summary>
		/// <param name="subscriber">The address of the subscriber to be removed.</param>
		/// <param name="messageType">The type of message in which the subscriber is no longer interested.</param>
		/// <exception cref="ArgumentNullException"></exception>
		void Remove(Uri subscriber, Type messageType);
	}
}