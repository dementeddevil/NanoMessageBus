namespace NanoMessageBus.RabbitChannel
{
	using RabbitMQ.Client.Events;

	public class RabbitMessage
	{
		public RabbitMessage(BasicDeliverEventArgs delivery) : this()
		{
		}
		protected RabbitMessage()
		{
		}
	}
}