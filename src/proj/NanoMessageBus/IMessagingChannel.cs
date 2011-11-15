namespace NanoMessageBus
{
	using System;

	/// <summary>
	/// Represents partition used to separate activities over a single connection to messaging infrastructure.
	/// </summary>
	/// <remarks>
	/// Instances of this class are single threaded and should not be shared between threads.
	/// </remarks>
	public interface IMessagingChannel : IDisposable
	{
		/// <summary>
		/// Gets the value which uniquely identifies the group to which this channel belongs.
		/// </summary>
		string ChannelGroup { get; }

		/// <summary>
		/// Gets the current inbound message being handled on the channel.
		/// </summary>
		EnvelopeMessage CurrentMessage { get; }

		/// <summary>
		/// Gets the current transaction associated with the channel, if transactions are available.
		/// </summary>
		IChannelTransaction CurrentTransaction { get; }

		/// <summary>
		/// Sends the message specified to the destinations provided.
		/// </summary>
		/// <param name="envelope">The envelope containing the messages to be sent.</param>
		/// <param name="destinations">The destinations to which the envelope should be sent.</param>
		/// <remarks>
		/// When no destinations are provided, the envelope is to be re-enqueued at the same location.
		/// </remarks>
		void Send(EnvelopeMessage envelope, params Uri[] destinations);

		/// <summary>
		/// Attempts to receive a message from the channel.
		/// </summary>
		/// <param name="timeout">The amount of time to wait before giving up.</param>
		/// <returns>If a message was received within the timeout specified, it is returned; otherwise null.</returns>
		EnvelopeMessage Receive(TimeSpan timeout);
	}
}