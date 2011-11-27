namespace NanoMessageBus
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Represents an exception that occurs when an expired message is received or a message is handled
	/// which doesn't have any handlers configured.
	/// </summary>
	[Serializable]
	public class DeadLetterException : ChannelException
	{
		/// <summary>
		/// Initializes a new instance of the DeadLetterException class.
		/// </summary>
		public DeadLetterException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the DeadLetterException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public DeadLetterException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the DeadLetterException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="inner">The exception that is the cause of the current exception.</param>
		public DeadLetterException(string message, Exception inner)
			: base(message, inner)
		{
		}

		/// <summary>
		/// Initializes a new instance of the DeadLetterException class.
		/// </summary>
		/// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
		/// <param name="context">The StreamingContext that holds contextual information about the source or destination.</param>
		protected DeadLetterException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}