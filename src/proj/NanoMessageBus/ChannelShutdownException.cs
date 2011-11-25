namespace NanoMessageBus
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Represents an exception that is thrown when any send or receive operations are attempted against a channel
	/// which is shutting down or has been shutdown.
	/// </summary>
	[Serializable]
	public class ChannelShutdownException : ChannelException
	{
		/// <summary>
		/// Initializes a new instance of the ChannelShutdownException class.
		/// </summary>
		public ChannelShutdownException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the ChannelShutdownException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public ChannelShutdownException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the ChannelShutdownException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="inner">The exception that is the cause of the current exception.</param>
		public ChannelShutdownException(string message, Exception inner)
			: base(message, inner)
		{
		}

		/// <summary>
		/// Initializes a new instance of the ChannelShutdownException class.
		/// </summary>
		/// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
		/// <param name="context">The StreamingContext that holds contextual information about the source or destination.</param>
		protected ChannelShutdownException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}