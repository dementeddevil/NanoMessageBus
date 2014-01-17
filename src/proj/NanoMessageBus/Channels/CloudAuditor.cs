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
			headers[FacilityNameHeader] = FacilityName;
			headers[MachineIdHeader] = MachineId;
		}

		public CloudAuditor()
		{
			this.cloud = !string.IsNullOrEmpty(FacilityName) && !string.IsNullOrEmpty(MachineId);
		}
		~CloudAuditor()
		{
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

		private const string FacilityNameHeader = "x-cloud-facility";
		private const string MachineIdHeader = "x-cloud-machine";
		private static readonly string FacilityName = CloudDetection.DetectFacility() ?? string.Empty;
		private static readonly string MachineId = CloudDetection.DetectMachineId() ?? string.Empty;
		private readonly bool cloud;
	}
}
