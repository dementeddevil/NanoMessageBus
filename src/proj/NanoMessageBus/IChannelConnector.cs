namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Security;

	/// <summary>
	/// Provides the ability to open, establish, and maintain a connection to the messaging infrastructure.
	/// </summary>
	/// <remarks>
	/// Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
	/// </remarks>
	public interface IChannelConnector : IDisposable
	{
		/// <summary>
		/// Gets the set of values which uniquely identify the channel groups to be created.
		/// </summary>
		IEnumerable<IChannelConfiguration> ChannelGroups { get; }

		/// <summary>
		/// Opens a channel against the underlying connection.
		/// </summary>
		/// <param name="channelGroup">The channel group indicating how the channel is to be configured.</param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="ChannelUnavailableException"></exception>
		/// <exception cref="SecurityException"></exception>
		/// <returns>An open channel through which messages may be sent or received according to the configuration.</returns>
		IMessagingChannel Connect(string channelGroup);
	}
}