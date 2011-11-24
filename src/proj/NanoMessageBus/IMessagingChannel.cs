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
		/// Begins receiving messages from the channel and dispatches them to the callback provided.
		/// </summary>
		/// <param name="callback">The callback to which the received message should be dispatched.</param>
		/// <exception cref="ChannelConnectionException"></exception>
		/// <remarks>
		/// The timeout, if any, has been specified as part of the channel configuration.
		/// </remarks>
		void BeginReceive(Action<IDeliveryContext> callback);
	}
}