namespace NanoMessageBus.Channels
{
	using System;

	public class CloudAuditor : IMessageAuditor
	{
		public void AuditReceive(IDeliveryContext delivery)
		{
			// no op
		}
		public void AuditSend(ChannelEnvelope envelope, IDeliveryContext delivery)
		{
			if (!_cloud)
			{
			    return;
			}

		    if (envelope == null)
		    {
		        throw new ArgumentNullException(nameof(envelope));
		    }

		    if (delivery != null && delivery.CurrentMessage == envelope.Message)
		    {
		        return;
		    }

		    var message = envelope.Message;
			if (message == envelope.State)
			{
			    return;
			}

		    var headers = message.Headers;
			headers[ProviderNameHeader] = ProviderName;
			headers[FacilityNameHeader] = FacilityName;
			headers[MachineIdHeader] = MachineId;
		}

		public CloudAuditor()
		{
			_cloud = FacilityName.Length > 0 && MachineId.Length > 0;
		}
		~CloudAuditor()
		{
			Dispose(false);
		}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			// no op
		}

		private const string ProviderNameHeader = "x-audit-cloud-provider";
		private const string ProviderName = "aws";
		private const string FacilityNameHeader = "x-audit-cloud-facility";
		private const string MachineIdHeader = "x-audit-cloud-machine-id";
		private static readonly string FacilityName = CloudDetection.DetectFacility();
		private static readonly string MachineId = CloudDetection.DetectMachineId();
		private readonly bool _cloud;
	}
}
