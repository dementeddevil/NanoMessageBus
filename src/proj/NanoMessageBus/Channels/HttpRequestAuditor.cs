namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections.Generic;
	using System.Web;

	public class HttpRequestAuditor : IMessageAuditor
	{
		public virtual void AuditReceive(IDeliveryContext delivery)
		{
			// no op
		}
		public virtual void AuditSend(ChannelEnvelope envelope)
		{
			var current = this.GetCurrentContext(envelope);
			if (current == null)
				return;

			var headers = envelope.Message.Headers;
			var request = current.Request;

			AppendHeader(headers, "useragent", request.UserAgent);
			AppendHeader(headers, "client-ip", GetUserAddress(request));
			AppendHeader(headers, "raw-url", request.RawUrl);
			AppendHeader(headers, "http-method", request.HttpMethod);
			AppendHeader(headers, "referring-url", AsString(request.UrlReferrer));
			AppendHeader(headers, "request-stamp", current.Timestamp.ToUniversalTime().ToIsoString());
		}
		private HttpContextBase GetCurrentContext(ChannelEnvelope envelope)
		{
			if (this.httpContext != null)
				return this.httpContext;

			if (envelope.State == null)
				return null;

			var context = envelope.State as HttpContext;
			return context == null ? envelope.State as HttpContextBase : new HttpContextWrapper(context);
		}
		private static string GetUserAddress(HttpRequestBase request)
		{
			var forwarded = request.Headers[ProxiedClient];
			if (string.IsNullOrEmpty(forwarded))
				return request.UserHostAddress;

			var address = request.UserHostAddress ?? string.Empty;
			if (forwarded.StartsWith(address, StringComparison.InvariantCultureIgnoreCase))
				return forwarded;

			return UserAddressFormat.FormatWith(address, forwarded);
		}
		private static void AppendHeader(IDictionary<string, string> headers, string key, string value)
		{
			if (!string.IsNullOrEmpty(value))
				headers.TrySetValue(HeaderFormat.FormatWith(key), value);
		}
		private static string AsString(object value)
		{
			return value == null ? null : value.ToString();
		}

		public HttpRequestAuditor(HttpContextBase httpContext)
		{
			this.httpContext = httpContext;
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
		private const string ProxiedClient = "X-Forwarded-For";
		private const string UserAddressFormat = "{0}, {1}";
		private readonly HttpContextBase httpContext;
	}
}