namespace NanoMessageBus
{
	using System;

	/// <summary>
	/// Represents an exception that is thrown when any send or receive operations are attempted against a channel
	/// which is shutting down or has been shutdown.
	/// </summary>
	[Serializable]
	public class ChannelShutdownException : ChannelException
	{
	}
}