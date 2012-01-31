namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Provides the ability to build a channel message.
	/// </summary>
	/// <remarks>
	/// Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
	/// </remarks>
	public interface IChannelMessageBuilder
	{
		/// <summary>
		/// Builds a new instance of the ChannelMessage class.
		/// </summary>
		/// <param name="correlationId">The value which uniquely identifies the conversation to which the message belongs.</param>
		/// <param name="returnAddress">The address to which all replies should be directed.</param>
		/// <param name="headers">The set of headers or metadata the ride along with the message.</param>
		/// <param name="messages">The set of logical, application-level messages to be dispatched.</param>
		/// <returns>A new instance of the ChannelMessage class.</returns>
		ChannelMessage Build(
			Guid correlationId,
			Uri returnAddress,
			IDictionary<string, string> headers,
			object[] messages);
	}
}