namespace NanoMessageBus
{
	/// <summary>
	/// Provides the ability to wrap the delivery of a message.
	/// </summary>
	public interface IDeliveryHandler
	{
		/// <summary>
		/// Handles the delivery.
		/// </summary>
		/// <param name="delivery">The delivery context to be handled.</param>
		void Handle(IDeliveryContext delivery);
	}
}