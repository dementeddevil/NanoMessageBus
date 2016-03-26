﻿using System.Threading.Tasks;

namespace NanoMessageBus
{
	using System;

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
		/// Gets a value indicating whether the channel group is a dispatch-only (non-receiving) group.
		/// </summary>
		bool DispatchOnly { get; }

		/// <summary>
		/// Starts up the underlying connector, initializes all channels associated with the group,
		/// and otherwise prepares the channel group to process and dispatch messages.
		/// </summary>
		/// <exception cref="ObjectDisposedException"></exception>
		void Initialize();

		/// <summary>
		/// Creates a messaging channel that is not controlled or tracked by channel group and which is owned
		/// and controlled by the caller.
		/// </summary>
		/// <exception cref="ChannelConnectionException"></exception>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="ObjectDisposedException"></exception>
		/// <returns>If the messaging infrastructure is available, it returns a reference to a new channel.</returns>
		IMessagingChannel OpenChannel();

		/// <summary>
		/// Begins streaming any available inbound messages to the callback provided; for dispatch-only groups
		/// it throws an exception.
		/// </summary>
		/// <param name="callback">The callback to which any received messages should be dispatched.</param>
		/// <exception cref="InvalidOperationException"></exception>
		void BeginReceive(Func<IDeliveryContext, Task> callback);

		/// <summary>
		/// For dispatch-only channel groups, it adds the callback provided to an in-memory queue for
		/// asynchronous invocation; for full-duplex channel groups (send/receive), it throws an exception.
		/// </summary>
		/// <param name="callback">The callback which creates and prepares a message for dispatch.</param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="InvalidOperationException"></exception>
		/// <returns>A value indicating whether or not the channel could add the item to the in-memory queue.</returns>
		bool BeginDispatch(Action<IDispatchContext> callback);
	}
}