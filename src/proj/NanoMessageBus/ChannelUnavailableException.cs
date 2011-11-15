namespace NanoMessageBus
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Represents a persistent communcation failure during a channel message exchange.
	/// </summary>
	[Serializable]
	public class ChannelUnavailableException : ChannelException
	{
		/// <summary>
		/// Initializes a new instance of the ChannelUnavailableException class.
		/// </summary>
		public ChannelUnavailableException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the ChannelUnavailableException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public ChannelUnavailableException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the ChannelUnavailableException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="inner">The exception that is the cause of the current exception.</param>
		public ChannelUnavailableException(string message, Exception inner)
			: base(message, inner)
		{
		}

		/// <summary>
		/// Initializes a new instance of the ChannelUnavailableException class.
		/// </summary>
		/// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
		/// <param name="context">The StreamingContext that holds contextual information about the source or destination.</param>
		protected ChannelUnavailableException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}