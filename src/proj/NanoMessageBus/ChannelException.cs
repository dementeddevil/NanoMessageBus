namespace NanoMessageBus
{
	using System;

	/// <summary>
	/// Represents a transient communication failure during a channel message exchange.
	/// </summary>
	[Serializable]
	public class ChannelException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the ChannelException class.
		/// </summary>
		public ChannelException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the ChannelException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="inner">The exception that is the cause of the current exception.</param>
		public ChannelException(string message, Exception inner)
			: base(message, inner)
		{
		}
	}
}