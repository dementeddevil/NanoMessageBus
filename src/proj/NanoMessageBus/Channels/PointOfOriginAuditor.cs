namespace NanoMessageBus.Channels
{
	using System;
	using System.Diagnostics;
	using System.Globalization;

	public class PointOfOriginAuditor : IMessageAuditor
	{
		public virtual void AuditReceive(IDeliveryContext delivery)
		{
			if (delivery == null)
			{
			    throw new ArgumentNullException(nameof(delivery));
			}

		    var header = delivery.CurrentMessage.Headers.TryGetValue(DispatchStamp);

			DateTime dispatched;
			if (DateTime.TryParse(header, out dispatched))
			{
			    delivery.CurrentMessage.Dispatched = dispatched.ToUniversalTime();
			}
		}
		public virtual void AuditSend(ChannelEnvelope envelope, IDeliveryContext delivery)
		{
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

		    var process = Process.GetCurrentProcess();
			var headers = message.Headers;
			headers[DispatchStamp] = SystemTime.UtcNow.ToIsoString();
			headers[OriginHost] = Environment.MachineName.ToLowerInvariant();
			headers[ProcessName] = process.ProcessName;
			headers[ProcessId] = process.Id.ToString(CultureInfo.InvariantCulture);
		}

		public PointOfOriginAuditor()
		{
		}
		~PointOfOriginAuditor()
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

		private const string HeaderFormat = "x-audit-{0}";
		private static readonly string OriginHost = HeaderFormat.FormatWith("origin-host");
		private static readonly string DispatchStamp = HeaderFormat.FormatWith("dispatched");
		private static readonly string ProcessId = HeaderFormat.FormatWith("origin-process-id");
		private static readonly string ProcessName = HeaderFormat.FormatWith("origin-process-name");
	}
}