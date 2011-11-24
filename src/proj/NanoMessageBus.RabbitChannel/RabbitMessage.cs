namespace NanoMessageBus.RabbitChannel
{
	using RabbitMQ.Client.Events;

	public class RabbitMessage
	{
		internal BasicDeliverEventArgs Delivery { get; set; }

		public RabbitMessage(object delivery)
			: this(delivery as BasicDeliverEventArgs)
		{
		}
		internal RabbitMessage(BasicDeliverEventArgs delivery) : this()
		{
			this.Delivery = delivery;
		}
		public RabbitMessage()
		{
		}
	}
}