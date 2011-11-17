namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents the primary, high-level interface for working with sending and receiving messages.
	/// </summary>
	/// <remarks>
	/// Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
	/// </remarks>
	public interface IMessagingHost : IDisposable
	{
		/// <summary>
		/// Creates all channel groups and the initializes each of them.
		/// </summary>
		void Initialize();

		/// <summary>
		/// Begins streaming any available inbound messages to the callback provided.
		/// </summary>
		/// <param name="callback">The callback to which any received messages should be dispatched.</param>
		void BeginReceive(Action<IMessagingChannel> callback);

		/// <summary>
		/// For dispatch-only channel groups, it adds the message provided to an in-memory queue for
		/// asynchronous dispatch; for full-duplex channel groups (send/receive), it throws an exception.
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="KeyNotFoundException"></exception>
		/// <param name="message">The message to be dispatched.</param>
		/// <param name="channelGroup">The channel group into which the message will be dispatched.</param>
		void BeginDispatch(ChannelMessage message, string channelGroup);

		/// <summary>
		/// For dispatch-only channel groups, it blocks the current thread while dispatching the message provided;
		/// for full-duplex channel groups (send/receive), it throws an exception.
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="KeyNotFoundException"></exception>
		/// <param name="message">The message to be dispatched.</param>
		/// <param name="channelGroup">The channel group into which the message will be dispatched.</param>
		void Dispatch(ChannelMessage message, string channelGroup);
	}
}