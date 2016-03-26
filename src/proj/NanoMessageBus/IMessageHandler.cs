using System.Threading.Tasks;

namespace NanoMessageBus
{
	/// <summary>
	/// Provides the ability to understand and handle a logical message.
	/// </summary>
	/// <typeparam name="T">The type of message to be handled.</typeparam>
	/// <remarks>
	/// Instances of this class may be either single or multi-threaded depending upon their registration.
	/// </remarks>
	public interface IMessageHandler<in T>
	{
		/// <summary>
		/// Handles the message provided.
		/// </summary>
		/// <param name="message">The message to be handled.</param>
        Task HandleAsync(T message);
    }
}