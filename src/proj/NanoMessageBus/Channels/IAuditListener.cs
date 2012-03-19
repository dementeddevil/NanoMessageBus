namespace NanoMessageBus.Channels
{
	using System;

	public interface IAuditListener : IDisposable
	{
		void AuditReceive(IDeliveryContext delivery);
		void AuditSend(ChannelEnvelope envelope);
	}
}