namespace NanoMessageBus
{
	using System;

	/// <summary>
	/// Represents a set of channels which operate connect to the same physical endpoint location
	/// and which operate as a group.
	/// </summary>
	/// <remarks>
	/// Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
	/// </remarks>
	public interface IChannelGroup : IDisposable
	{
		/// <summary>
		/// Gets the name which uniquely identifies the channel group.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Starts up all of the underlying connectors, opens up all channels, and otherwise prepares the
		/// messaging host to process and dispatch messages.
		/// </summary>
		void Initialize();

		/// <summary>
		/// Begins streaming any available inbound messages to the callback provided to the channel configuration.
		/// </summary>
		void BeginReceive();

		/// <summary>
		/// For dispatch-only channel groups, it adds the message provided to an in-memory queue for
		/// asynchronous dispatch; for full-duplex channel groups (send/receive), it throws an
		/// InvalidOperationException.
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		/// <param name="envelope">The message envelope to be dispatched.</param>
		void BeginDispatch(EnvelopeMessage envelope);

		/// <summary>
		/// For dispatch-only channel groups, it blocks the current thread while dispatching the message provided;
		/// for full-duplex channel groups (send/receive), it throws an InvalidOperationException.
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		/// <param name="envelope">The message envelope to be dispatched.</param>
		void Dispatch(EnvelopeMessage envelope);
	}
}