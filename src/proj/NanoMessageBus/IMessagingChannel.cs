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
		/// Receive the message from the channel, if any, and dispatches it to the callback provided.
		/// </summary>
		/// <param name="callback">The callback to which the received message should be dispatched.</param>
		/// <exception cref="ChannelConnectionException"></exception>
		/// <remarks>
		/// The timeout, if any, has been specified as part of the channel configuration.
		/// </remarks>
		void Receive(Action<IMessagingChannel> callback);
	}
}