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
	public interface IChannelGroup
	{
		/// <summary>
		/// Gets the name which uniquely identifies the channel group.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets a value indicating whether the channel group is configured for dispatch-only or
		/// full-duplex (send/receive) operation.
		/// </summary>
		bool DispatchOnly { get; }

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