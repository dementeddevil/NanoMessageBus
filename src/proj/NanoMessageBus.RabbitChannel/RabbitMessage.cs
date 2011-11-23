namespace NanoMessageBus.RabbitChannel
{
	using RabbitMQ.Client.Events;

	public class RabbitMessage
	{
		public BasicDeliverEventArgs Delivery { get; set; }

		public RabbitMessage(BasicDeliverEventArgs delivery) : this()
		{
			this.Delivery = delivery;
		}
		protected RabbitMessage()
		{
		}
	}
}