namespace NanoMessageBus.Channels
{
	using System;

	public class PointOfOriginAuditor : IMessageAuditor
	{
		public virtual void AuditReceive(IDeliveryContext delivery)
		{
			if (delivery == null)
				throw new ArgumentNullException("delivery");

			var header = delivery.CurrentMessage.Headers.TryGetValue(DispatchStamp);

			DateTime dispatched;
			if (DateTime.TryParse(header, out dispatched))
				delivery.CurrentMessage.Dispatched = dispatched.ToUniversalTime();
		}
		public virtual void AuditSend(ChannelEnvelope envelope)
		{
			if (envelope == null)
				throw new ArgumentNullException("envelope");

			var headers = envelope.Message.Headers;
			headers[OriginHost] = Environment.MachineName.ToLowerInvariant();
			headers[DispatchStamp] = SystemTime.UtcNow.ToIsoString();
		}

		public PointOfOriginAuditor()
		{
		}
		~PointOfOriginAuditor()
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

		private const string HeaderFormat = "x-audit-{0}";
		private static readonly string OriginHost = HeaderFormat.FormatWith("origin-host");
		private static readonly string DispatchStamp = HeaderFormat.FormatWith("dispatched");
	}
}