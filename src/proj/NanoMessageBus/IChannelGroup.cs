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
	public interface IChannelGroup : IChannelDispatch, IDisposable
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
	}
}