namespace NanoMessageBus
{
	using System;

	/// <summary>
	/// Represents the delivery of a single message on a particular channel.  Where transactional messaging
	/// is a available, the send operation will occur within the bounds of the receiving transaction.
	/// </summary>
	/// <remarks>
	/// Instances of this class are single threaded and should not be shared between threads.
	/// </remarks>
	public interface IDeliveryContext
	{
		/// <summary>
		/// Gets the current inbound message being handled on the channel.
		/// </summary>
		ChannelMessage CurrentMessage { get; }

		/// <summary>
		/// Gets the current transaction associated with the channel, if transactions are available.
		/// </summary>
		IChannelTransaction CurrentTransaction { get; }

		/// <summary>
		/// Sends the message specified to the destinations provided.
		/// </summary>
		/// <param name="message">The message containing the logical messages to be sent.</param>
		/// <param name="destinations">The destinations to which the message should be sent.</param>
		/// <remarks>
		/// When no destinations are provided, the message is to be re-enqueued at the same location.
		/// </remarks>
		void Send(ChannelMessage message, params Uri[] destinations);
	}
}