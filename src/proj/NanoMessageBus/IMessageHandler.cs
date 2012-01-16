namespace NanoMessageBus
{
	/// <summary>
	/// Provides the ability to understand and handle a logical message.
	/// </summary>
	/// <typeparam name="T">The type of message to be handled.</typeparam>
	public interface IMessageHandler<in T>
	{
		/// <summary>
		/// Handles the message provided.
		/// </summary>
		/// <param name="message">The message to be handled.</param>
		void Handle(T message);
	}
}