namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents a set of channels which operate connect to the same physical endpoint location and which
	/// operate as a cohesive unit.
	/// </summary>
	/// <remarks>
	/// Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
	/// </remarks>
	public interface IChannelGroup : IDisposable
	{
		/// <summary>
		/// Starts up all of the underlying connectors, initializes all channels associated with the group,
		/// and otherwise prepares the channel group to process and dispatch messages.
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
		/// <param name="message">The message to be dispatched.</param>
		/// <param name="recipients">The recipients to whom a copy of the message will be dispatched.</param>
		void BeginDispatch(ChannelMessage message, IEnumerable<Uri> recipients);

		/// <summary>
		/// For dispatch-only channel groups, it blocks the current thread while dispatching the message provided;
		/// for full-duplex channel groups (send/receive), it throws an exception.
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		/// <param name="message">The message to be dispatched.</param>
		/// <param name="recipients">The recipients to whom a copy of the message will be dispatched.</param>
		void Dispatch(ChannelMessage message, IEnumerable<Uri> recipients);
	}
}