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

			AppendHeader(headers, "useragent", request.UserAgent);
			AppendHeader(headers, "client-ip", this.UserAddress);
			AppendHeader(headers, "raw-url", request.RawUrl);
			AppendHeader(headers, "http-method", request.HttpMethod);
			AppendHeader(headers, "referring-url", AsString(request.UrlReferrer));
			AppendHeader(headers, "request-stamp", this.context.Timestamp.ToIsoString());
		}
		private static void AppendHeader(IDictionary<string, string> headers, string key, string value)
		{
			headers.TrySetValue(HeaderFormat.FormatWith(key), value);
		}
		private static string AsString(object value)
		{
			return value == null ? null : value.ToString();
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
		private const string ProxiedClient = "X-Forwarded-For";
		private const string UserAddressFormat = "{0}, {1}";
		private readonly HttpContextBase context;
	}
}