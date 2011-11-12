namespace NanoMessageBus.Endpoints
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Represents an error when communication with the endpoint has failed.
	/// </summary>
	[Serializable]
	public class EndpointUnavailableException : EndpointException
	{
		/// <summary>
		/// Initializes a new instance of the EndpointUnavailableException class.
		/// </summary>
		public EndpointUnavailableException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the EndpointUnavailableException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public EndpointUnavailableException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the EndpointUnavailableException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="inner">The exception that is the cause of the current exception.</param>
		public EndpointUnavailableException(string message, Exception inner)
			: base(message, inner)
		{
		}

		/// <summary>
		/// Initializes a new instance of the EndpointUnavailableException class.
		/// </summary>
		/// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
		/// <param name="context">The StreamingContext that holds contextual information about the source or destination.</param>
		protected EndpointUnavailableException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}