namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections.Generic;
	using System.Web;

	public class HttpRequestAuditor : IMessageAuditor
	{
		private string UserAddress
		{
			get
			{
				var request = this.context.Request;

				var forwarded = request.Headers[ProxiedClient];
				if (string.IsNullOrEmpty(forwarded))
					return request.UserHostAddress;

				return UserAddressFormat.FormatWith(request.UserHostAddress, forwarded);
			}
		}

		public virtual void AuditReceive(IDeliveryContext delivery)
		{
			// no op
		}
		public virtual void AuditSend(ChannelEnvelope envelope)
		{
			var headers = envelope.Message.Headers;
			var request = this.context.Request;

			AppendHeaders(headers, "useragent", request.UserAgent);
			AppendHeaders(headers, "client-ip", this.UserAddress);
			AppendHeaders(headers, "raw-url", request.RawUrl);
			AppendHeaders(headers, "http-method", request.HttpMethod);
			AppendHeaders(headers, "referring-url", request.UrlReferrer.AsString());
			AppendHeaders(headers, "request-stamp", this.context.Timestamp.ToString(Iso8601));
		}
		private static void AppendHeaders(IDictionary<string, string> headers, string key, string value)
		{
			headers.TrySetValue(HeaderFormat.FormatWith(key), value);
		}

		public HttpRequestAuditor(HttpContextBase context)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			
			this.context = context;
		}
		~HttpRequestAuditor()
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
		}

		private const string HeaderFormat = "x-audit-{0}";
		private const string Iso8601 = "o";
		private const string ProxiedClient = "X-Forwarded-For";
		private const string UserAddressFormat = "{0}, {1}";
		private readonly HttpContextBase context;
	}
}