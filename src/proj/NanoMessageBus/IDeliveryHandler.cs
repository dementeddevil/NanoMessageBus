namespace NanoMessageBus
{
	/// <summary>
	/// Provides the ability to wrap the delivery of a message.
	/// </summary>
	/// <remarks>
	/// Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
	/// </remarks>
	public interface IDeliveryHandler : IMessageHandler<IDeliveryContext>
	{
	}
}