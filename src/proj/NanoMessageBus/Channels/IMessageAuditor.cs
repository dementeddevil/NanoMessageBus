namespace NanoMessageBus.Channels
{
	using System;

	public interface IMessageAuditor : IDisposable
	{
		void AuditReceive(IDeliveryContext delivery);
		void AuditSend(ChannelEnvelope envelope);
	}
}