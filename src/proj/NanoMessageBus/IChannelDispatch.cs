namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Provides the ability to dispatch a message either synchronously or asynchronously.
	/// </summary>
	/// <remarks>
	/// Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
	/// </remarks>
	public interface IChannelDispatch
	{
		/// <summary>
		/// For dispatch-only channel groups, it adds the message provided to an in-memory queue for
		/// asynchronous dispatch; for full-duplex channel groups (send/receive), it throws an exception.
		/// </summary>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="InvalidOperationException"></exception>
		/// <param name="message">The message to be dispatched.</param>
		/// <param name="recipients">The recipients to whom a copy of the message will be dispatched.</param>
		void BeginDispatch(ChannelMessage message, IEnumerable<Uri> recipients);

		/// <summary>
		/// For dispatch-only channel groups, it blocks the current thread while dispatching the message provided;
		/// for full-duplex channel groups (send/receive), it throws an exception.
		/// </summary>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="InvalidOperationException"></exception>
		/// <param name="message">The message to be dispatched.</param>
		/// <param name="recipients">The recipients to whom a copy of the message will be dispatched.</param>
		void Dispatch(ChannelMessage message, IEnumerable<Uri> recipients); 
	}
}