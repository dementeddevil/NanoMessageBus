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
			if (!this.cloud)
				return;

			if (envelope == null)
				throw new ArgumentNullException("envelope");

			if (delivery != null && delivery.CurrentMessage == envelope.Message)
				return;

			var message = envelope.Message;
			if (message == envelope.State)
				return;

			var headers = message.Headers;
			headers[ProviderNameHeader] = ProviderName;
			headers[FacilityNameHeader] = FacilityName;
			headers[MachineIdHeader] = MachineId;
		}

		public CloudAuditor()
		{
			this.cloud = FacilityName.Length > 0 && MachineId.Length > 0;
		}
		~CloudAuditor()
		{
			this.Dispose(false);
		}
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			// no op
		}

		private const string ProviderNameHeader = "x-cloud-provider";
		private const string ProviderName = "aws";
		private const string FacilityNameHeader = "x-cloud-facility";
		private const string MachineIdHeader = "x-cloud-machine-id";
		private static readonly string FacilityName = CloudDetection.DetectFacility();
		private static readonly string MachineId = CloudDetection.DetectMachineId();
		private readonly bool cloud;
	}
}
