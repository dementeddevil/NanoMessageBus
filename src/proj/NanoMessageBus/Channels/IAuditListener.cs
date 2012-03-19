namespace NanoMessageBus.Channels
{
	using System;

	public interface IAuditListener : IDisposable
	{
		void Audit(IDeliveryContext delivery);
		void Audit(ChannelEnvelope envelope);
	}
}