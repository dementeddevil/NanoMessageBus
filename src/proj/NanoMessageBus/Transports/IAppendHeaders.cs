namespace NanoMessageBus.Transports
{
	/// <summary>
	/// Provides the ability to append headers to outgoing messages.
	/// </summary>
	public interface IAppendHeaders
	{
		/// <summary>
		/// Appends headers to the outgoing message.
		/// </summary>
		/// <param name="message">The message to which the headers should be appended.</param>
		void AppendHeaders(EnvelopeMessage message);
	}
}