namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections.Generic;

	public class PointOfOriginAuditor : IMessageAuditor
	{
		public virtual void AuditReceive(IDeliveryContext delivery)
		{
		}
		public virtual void AuditSend(ChannelEnvelope envelope)
		{
			if (envelope == null)
				throw new ArgumentNullException("envelope");

			var headers = envelope.Message.Headers;
			AppendHeader(headers, "origin-host", Environment.MachineName.ToLowerInvariant());
			AppendHeader(headers, "dispatch-stamp", SystemTime.UtcNow.ToIsoString());
		}
		private static void AppendHeader(IDictionary<string, string> headers, string key, string value)
		{
			headers.TrySetValue(HeaderFormat.FormatWith(key), value);
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
	}
}