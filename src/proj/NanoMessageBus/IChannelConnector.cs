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
		/// Connects to the underlying infrastructure and establishes the configured channel groups.
		/// </summary>
		/// <exception cref="SecurityException"></exception>
		/// <exception cref="ChannelUnavailableException"></exception>
		/// <returns>A set of configured channel groups.</returns>
		IEnumerable<IChannelGroup> Connect();
	}
}