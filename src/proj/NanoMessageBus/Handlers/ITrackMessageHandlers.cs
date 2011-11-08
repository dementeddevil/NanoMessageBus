namespace NanoMessageBus.Handlers
{
	using System.Collections.Generic;

	/// <summary>
	/// Provides the ability to retrieve all message handlers for a given message.
	/// </summary>
	public interface ITrackMessageHandlers
	{
		/// <summary>
		/// Gets the configured handlers for the message provided.
		/// </summary>
		/// <param name="message">The message instance for which all handlers should be retreived.</param>
		/// <returns>Returns a set of handlers for the message provided.</returns>
		IEnumerable<IHandleMessages<object>> GetHandlers(object message);
	}
}