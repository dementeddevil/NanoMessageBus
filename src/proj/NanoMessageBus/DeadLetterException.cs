namespace NanoMessageBus
{
	using System;

	/// <summary>
	/// Represents an exception that occurs when an expired (dead on the wire) message is received.
	/// </summary>
	[Serializable]
	public class DeadLetterException : ChannelException
	{
		public DateTime Expiration { get; private set; }

		public DeadLetterException(DateTime expiration)
		{
			this.Expiration = expiration;
		}
	}
}