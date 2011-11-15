namespace NanoMessageBus
{
	using System;

	/// <summary>
	/// Represents the primary, high-level interface for working with sending and receiving messages.
	/// </summary>
	/// <remarks>
	/// Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
	/// </remarks>
	public interface IMessagingHost : IDisposable
	{
		/// <summary>
		/// Starts up all of the underlying connectors, opens up all channels and their associated
		/// channel groups, and otherwise prepares the messaging host to process and dispatch messages.
		/// </summary>
		void Initialize();

		/// <summary>
		/// Begins streaming the inbound messages to the callback provided.
		/// </summary>
		/// <param name="callback">The callback to which all message streams will be directed.</param>
		void BeginReceive(Action<IMessagingChannel, EnvelopeMessage> callback);

		/// <summary>
		/// Gets the channel group for the key specified, if it exists.
		/// </summary>
		/// <param name="key">The key which uniquely identifies the desired channel group.</param>
		/// <returns>If found, returns the requested channel group; otherwise, returns null.</returns>
		IChannelGroup this[string key] { get; }
	}
}