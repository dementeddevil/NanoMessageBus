namespace NanoMessageBus.RabbitChannel
{
	using System;
	using RabbitMQ.Client;

	public class RabbitAddress
	{
		public virtual PublicationAddress Address
		{
			get { return null; }
		}

		public RabbitAddress(Uri address) : this()
		{
		}
		protected RabbitAddress()
		{
		}
	}
}