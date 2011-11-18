namespace NanoMessageBus
{
	using System;

	/// <summary>
	/// Represents partition used to separate activities over a single connection to messaging infrastructure.
	/// </summary>
	/// <remarks>
	/// Instances of this class are single threaded and should not be shared between threads.
	/// </remarks>
	public interface IMessagingChannel : IDeliveryContext, IDisposable
	{
		/// <summary>
		/// Gets the value which uniquely identifies the group to which this channel belongs.
		/// </summary>
		string ChannelGroup { get; }

		/// <summary>
		/// Receive the message from the channel, if any, and dispatches it to the callback provided.
		/// </summary>
		/// <param name="callback">The callback to which the received message should be dispatched.</param>
		/// <exception cref="ChannelConnectionException"></exception>
		/// <remarks>
		/// The timeout, if any, has been specified as part of the channel configuration.
		/// </remarks>
		void Receive(Action<IDeliveryContext> callback);
	}
}