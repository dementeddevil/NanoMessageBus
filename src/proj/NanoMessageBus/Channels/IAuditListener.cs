namespace NanoMessageBus.Channels
{
	using System;

	public interface IAuditListener : IDisposable
	{
		void Receive(IDeliveryContext context);
		void Send(ChannelMessage message);
	}
}