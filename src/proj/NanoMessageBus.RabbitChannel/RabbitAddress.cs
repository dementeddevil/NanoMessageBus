namespace NanoMessageBus.RabbitChannel
{
	using System;
	using RabbitMQ.Client;

	public class RabbitAddress : PublicationAddress
	{
		public RabbitAddress(Uri address) : this()
		{
		}
		protected RabbitAddress()
			: base(string.Empty, string.Empty, string.Empty)
		{
		}
	}
}