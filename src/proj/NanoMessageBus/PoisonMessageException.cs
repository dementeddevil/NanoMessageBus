namespace NanoMessageBus
{
	using System;

	/// <summary>
	/// Represents an exception that occurs when a message cannot be properly deserialized or when
	/// a component in the message pipeline decides the message should cannot be successfully processed.
	/// </summary>
	[Serializable]
	public class PoisonMessageException : ChannelException
	{
		public PoisonMessageException()
		{
		}
		public PoisonMessageException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}