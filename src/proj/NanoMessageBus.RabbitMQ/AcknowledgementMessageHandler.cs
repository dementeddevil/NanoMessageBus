namespace NanoMessageBus.RabbitMQ
{
	public class AcknowledgementMessageHandler : IHandleMessages<EnvelopeMessage>
	{
		public void Handle(EnvelopeMessage message)
		{
			this.delivery.AcknowledgeDelivery();
		}

		public AcknowledgementMessageHandler(DeliveryContext delivery)
		{
			this.delivery = delivery;
		}

		private readonly DeliveryContext delivery;
	}
}