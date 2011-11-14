namespace NanoMessageBus.Channels
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Represents a transient communcation failure during a channel message exchange.
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
		public ChannelException(string message)
			: base(message)
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

		/// <summary>
		/// Initializes a new instance of the ChannelException class.
		/// </summary>
		/// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
		/// <param name="context">The StreamingContext that holds contextual information about the source or destination.</param>
		protected ChannelException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}