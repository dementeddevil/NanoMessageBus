namespace NanoMessageBus
{
	/// <summary>
	/// Provides the ability to wrap the delivery of a message.
	/// </summary>
	public interface IDeliveryHandler : IMessageHandler<IDeliveryContext>
	{
	}
}