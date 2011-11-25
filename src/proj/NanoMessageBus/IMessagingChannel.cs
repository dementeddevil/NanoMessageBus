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
		/// Initiates the process shutting down the channel to prevent additional sends and/or receives
		/// from occurring on the channel.
		/// </summary>
		/// <remarks>
		/// This is the only thread-safe method that can be invoked on the channel.
		/// </remarks>
		void BeginShutdown();

		/// <summary>
		/// Begins receiving messages from the channel and dispatches them to the callback provided.
		/// </summary>
		/// <param name="callback">The callback to which the received message should be dispatched.</param>
		/// <exception cref="ChannelConnectionException"></exception>
		/// <exception cref="ChannelShutdownException"></exception>
		/// <remarks>
		/// The timeout, if any, has been specified as part of the channel configuration.
		/// </remarks>
		void Receive(Action<IDeliveryContext> callback);
	}
}