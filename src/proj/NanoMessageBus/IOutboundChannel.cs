namespace NanoMessageBus
{
	using System;

	/// <summary>
	/// Provides the ability to dispatch a message either synchronously or asynchronously.
	/// </summary>
	/// <remarks>
	/// Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
	/// </remarks>
	public interface IOutboundChannel
	{
		/// <summary>
		/// For dispatch-only channel groups, it adds the message provided to an in-memory queue for
		/// asynchronous dispatch; for full-duplex channel groups (send/receive), it throws an exception.
		/// </summary>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="InvalidOperationException"></exception>
		/// <param name="envelope">The envelope which contains the message and set of intended recipients.</param>
		/// <param name="completed">The callback to be invoked when the dispatch has completed.</param>
		void BeginDispatch(ChannelEnvelope envelope, Action completed);

		/// <summary>
		/// For dispatch-only channel groups, it blocks the current thread while dispatching the message provided;
		/// for full-duplex channel groups (send/receive), it throws an exception.
		/// </summary>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="InvalidOperationException"></exception>
		/// <param name="envelope">The envelope which contains the message and set of intended recipients.</param>
		void Dispatch(ChannelEnvelope envelope); 
	}
}