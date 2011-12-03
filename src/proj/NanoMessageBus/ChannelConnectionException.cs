namespace NanoMessageBus
{
	using System;

	/// <summary>
	/// Represents a persistent communcation failure during a channel message exchange.
	/// </summary>
	[Serializable]
	public class ChannelConnectionException : ChannelException
	{
		/// <summary>
		/// Initializes a new instance of the ChannelConnectionException class.
		/// </summary>
		public ChannelConnectionException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the ChannelConnectionException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="inner">The exception that is the cause of the current exception.</param>
		public ChannelConnectionException(string message, Exception inner)
			: base(message, inner)
		{
		}
	}
}