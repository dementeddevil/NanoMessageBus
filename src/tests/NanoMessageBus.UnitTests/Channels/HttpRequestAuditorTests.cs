#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.Web;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject(typeof(HttpRequestAuditor))]
	public class when_a_null_http_context_is_provided : using_an_http_request_auditor
	{
		Because of = () =>
			Try(() => new HttpRequestAuditor(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(HttpRequestAuditor))]
	public class when_is_being_received : using_an_http_request_auditor
	{
		Because of = () =>
			auditor.AuditReceive(null);

		It should_do_nothing = () =>
			thrown.ShouldBeNull();
	}

	[Subject(typeof(HttpRequestAuditor))]
	public class when_a_message_is_being_sent : using_an_http_request_auditor
	{
		Establish context = () =>
		{
			mockRequest.Setup(x => x.UserAgent).Returns("MyBrowser");
			mockRequest.Setup(x => x.UserHostAddress).Returns("127.0.0.1");
			mockRequest.Setup(x => x.RawUrl).Returns("/raw-url/?#");
			mockRequest.Setup(x => x.HttpMethod).Returns("my-method");
			mockRequest.Setup(x => x.UrlReferrer).Returns(new Uri("http://domain.com/referer"));
			mockContext.Setup(x => x.Timestamp).Returns(DateTime.Parse("2010-01-01"));
		};

		Because of = () =>
			auditor.AuditSend(mockEnvelope.Object);

		It should_append_the_browser_useragent_to_the_outgoing_headers = () =>
			messageHeaders["x-audit-useragent"].ShouldEqual("MyBrowser");

		It should_append_the_client_ip_address_to_the_outgoing_headers = () =>
			messageHeaders["x-audit-client-ip"].ShouldEqual("127.0.0.1");

		It should_append_the_raw_url_to_the_outgoing_headers = () =>
			messageHeaders["x-audit-raw-url"].ShouldEqual("/raw-url/?#");

		It should_append_the_http_method_to_the_outgoing_headers = () =>
			messageHeaders["x-audit-http-method"].ShouldEqual("my-method");

		It should_append_the_referring_url_to_the_outgoing_headers = () =>
			messageHeaders["x-audit-referring-url"].ShouldEqual("http://domain.com/referer");

		It should_append_the_request_stamp_to_the_outgoing_headers = () =>
			messageHeaders["x-audit-request-stamp"].ShouldEqual("2010-01-01T00:00:00.0000000");
	}

	[Subject(typeof(HttpRequestAuditor))]
	public class when_a_message_is_being_sent_from_a_proxied_client_with_multiple_client_ips : using_an_http_request_auditor
	{
		Establish context = () =>
		{
			mockRequest.Setup(x => x.UserHostAddress).Returns("127.0.0.1");
			requestHeaders["X-Forwarded-For"] = "1.1.1.1, 2.2.2.2, 3.3.3.3";
		};

		Because of = () =>
			auditor.AuditSend(mockEnvelope.Object);

		It should_append_all_client_ip_address_to_the_outgoing_headers = () =>
			messageHeaders["x-audit-client-ip"].ShouldEqual("127.0.0.1, 1.1.1.1, 2.2.2.2, 3.3.3.3");
	}

	[Subject(typeof(HttpRequestAuditor))]
	public class when_the_auditor_is_disposed : using_an_http_request_auditor
	{
		Because of = () =>
			auditor.Dispose();

		It should_not_do_anything = () =>
			thrown.ShouldBeNull();
	}

	public abstract class using_an_http_request_auditor
	{
		Establish context = () =>
		{
			mockRequest = new Mock<HttpRequestBase>();
			mockContext = new Mock<HttpContextBase>();
			requestHeaders = new NameValueCollection();
			mockContext.Setup(x => x.Request).Returns(mockRequest.Object);
			mockRequest.Setup(x => x.Headers).Returns(requestHeaders);

			mockEnvelope = new Mock<ChannelEnvelope>();
			mockMessage = new Mock<ChannelMessage>();
			messageHeaders = new Dictionary<string, string>();
			mockEnvelope.Setup(x => x.Message).Returns(mockMessage.Object);
			mockMessage.Setup(x => x.Headers).Returns(messageHeaders);

			thrown = null;

			auditor = new HttpRequestAuditor(mockContext.Object);
		};

		protected static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		protected static HttpRequestAuditor auditor;
		protected static Mock<HttpContextBase> mockContext;
		protected static Mock<HttpRequestBase> mockRequest;
		protected static NameValueCollection requestHeaders;

		protected static Mock<ChannelMessage> mockMessage;
		protected static Mock<ChannelEnvelope> mockEnvelope;
		protected static IDictionary<string, string> messageHeaders;
		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169