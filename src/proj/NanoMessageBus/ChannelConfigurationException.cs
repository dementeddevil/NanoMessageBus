namespace NanoMessageBus
{
	using System;

	/// <summary>
	/// Represents a configuration issue related to the channel which prevents it from starting.
	/// </summary>
	[Serializable]
	public class ChannelConfigurationException : ChannelException
	{
		/// <summary>
		/// Initializes a new instance of the ChannelConfigurationException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="inner">The exception that is the cause of the current exception.</param>
		public ChannelConfigurationException(string message, Exception inner)
			: base(message, inner)
		{
		}
	}
}