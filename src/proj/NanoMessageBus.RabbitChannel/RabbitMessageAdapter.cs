namespace NanoMessageBus.RabbitChannel
{
	using RabbitMQ.Client.Events;
	using Serialization;

	public class RabbitMessageAdapter
	{
		public virtual ChannelMessage Build(BasicDeliverEventArgs message)
		{
			return null; // TODO
		}
		public virtual BasicDeliverEventArgs Build(ChannelMessage message)
		{
			return null; // TODO
		}
		public virtual void Release(BasicDeliverEventArgs message)
		{
			// TODO: stop tracking delivery
		}

		public RabbitMessageAdapter(ISerializer serializer) : this()
		{
			this.serializer = serializer;
		}
		protected RabbitMessageAdapter()
		{
		}

		private readonly ISerializer serializer;
	}
}