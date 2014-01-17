#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.Globalization;
	using System.Web;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject(typeof(HttpRequestAuditor))]
	public class when_a_null_http_context_is_provided : using_an_http_request_auditor
	{
		Because of = () =>
			Try(() => new HttpRequestAuditor(null));

		It should_NOT_throw_an_exception = () =>
			thrown.ShouldBeNull();
	}

	[Subject(typeof(HttpRequestAuditor))]
	public class when_a_message_is_being_received : using_an_http_request_auditor
	{
		Because of = () =>
			auditor.AuditReceive(null);

		It should_do_nothing = () =>
			thrown.ShouldBeNull();
	}

	[Subject(typeof(HttpRequestAuditor))]
	public class when_no_http_request_is_available : using_an_http_request_auditor
	{
		Establish context = () =>
			auditor = new HttpRequestAuditor(null);

		Because of = () =>
			auditor.AuditSend(mockEnvelope.Object, null);

		It should_not_append_anything_to_the_headers = () =>
			messageHeaders.Count.ShouldEqual(0);
	}

	[Subject(typeof(HttpRequestAuditor))]
	public class when_a_message_is_being_sent : using_an_http_request_auditor
	{
		Establish context = () =>
		{
			serverVariables["LOCAL_ADDR"] = "192.168.0.1";
			mockRequest.Setup(x => x.UserAgent).Returns("MyBrowser");
			mockRequest.Setup(x => x.UserHostAddress).Returns("127.0.0.1");
			mockRequest.Setup(x => x.RawUrl).Returns("/raw-url/?#");
			mockRequest.Setup(x => x.Url).Returns(new Uri("http://www.google.com/"));
			mockRequest.Setup(x => x.HttpMethod).Returns("my-method");
			mockRequest.Setup(x => x.UrlReferrer).Returns(new Uri("http://domain.com/referer"));
			mockContext.Setup(x => x.Timestamp).Returns(DateTime.Parse("2010-01-01", null, DateTimeStyles.AssumeUniversal));
		};

		Because of = () =>
			auditor.AuditSend(mockEnvelope.Object, null);

		It should_append_the_browser_useragent_to_the_outgoing_headers = () =>
			messageHeaders["x-audit-useragent"].ShouldEqual("MyBrowser");

		It should_append_the_client_ip_address_to_the_outgoing_headers = () =>
			messageHeaders["x-audit-client-ip"].ShouldEqual("127.0.0.1");

		It should_append_the_server_ip_address_to_the_outgoing_headers = () =>
			messageHeaders["x-audit-server-ip"].ShouldEqual("192.168.0.1");

		It should_append_the_raw_url_to_the_outgoing_headers = () =>
			messageHeaders["x-audit-raw-url"].ShouldEqual("/raw-url/?#");

		It should_append_the_hostname_to_the_outgoing_headers = () =>
			messageHeaders["x-audit-hostname"].ShouldEqual("www.google.com");

		It should_append_the_http_method_to_the_outgoing_headers = () =>
			messageHeaders["x-audit-http-method"].ShouldEqual("my-method");

		It should_append_the_referring_url_to_the_outgoing_headers = () =>
			messageHeaders["x-audit-referring-url"].ShouldEqual("http://domain.com/referer");

		It should_append_the_request_stamp_to_the_outgoing_headers = () =>
			messageHeaders["x-audit-request-stamp"].ShouldEqual("2010-01-01T00:00:00.0000000Z");
	}

	[Subject(typeof(HttpRequestAuditor))]
	public class when_a_given_value_on_the_request_is_empty : using_an_http_request_auditor
	{
		Establish context = () => 
			mockRequest.Setup(x => x.UserAgent).Returns((string)null);

		Because of = () =>
			auditor.AuditSend(mockEnvelope.Object, null);

		It should_not_append_the_header_to_the_message = () =>
			messageHeaders.ContainsKey("x-audit-useragent").ShouldBeFalse();
	}

	[Subject(typeof(HttpRequestAuditor))]
	public class when_the_http_context_is_available_as_part_of_the_envelope_state : using_an_http_request_auditor
	{
		Establish context = () =>
		{
			auditor = new HttpRequestAuditor(null);
			mockEnvelope.Setup(x => x.State).Returns(mockContext.Object);

			mockRequest.Setup(x => x.UserAgent).Returns("MyBrowser");
			mockRequest.Setup(x => x.UserHostAddress).Returns("127.0.0.1");
			mockRequest.Setup(x => x.RawUrl).Returns("/raw-url/?#");
			mockRequest.Setup(x => x.HttpMethod).Returns("my-method");
			mockRequest.Setup(x => x.UrlReferrer).Returns(new Uri("http://domain.com/referer"));
			mockContext.Setup(x => x.Timestamp).Returns(DateTime.Parse("2010-01-01", null, DateTimeStyles.AssumeUniversal));
		};

		Because of = () =>
			auditor.AuditSend(mockEnvelope.Object, null);

		It should_append_the_browser_useragent_from_the_envelope_state_to_the_outgoing_headers = () =>
			messageHeaders["x-audit-useragent"].ShouldEqual("MyBrowser");

		It should_append_the_client_ip_address_from_the_envelope_state_to_the_outgoing_headers = () =>
			messageHeaders["x-audit-client-ip"].ShouldEqual("127.0.0.1");

		It should_append_the_raw_url_from_the_envelope_state_to_the_outgoing_headers = () =>
			messageHeaders["x-audit-raw-url"].ShouldEqual("/raw-url/?#");

		It should_append_the_http_method_from_the_envelope_state_to_the_outgoing_headers = () =>
			messageHeaders["x-audit-http-method"].ShouldEqual("my-method");

		It should_append_the_referring_from_the_envelope_state_url_to_the_outgoing_headers = () =>
			messageHeaders["x-audit-referring-url"].ShouldEqual("http://domain.com/referer");

		It should_append_the_request_from_the_envelope_state_stamp_to_the_outgoing_headers = () =>
			messageHeaders["x-audit-request-stamp"].ShouldEqual("2010-01-01T00:00:00.0000000Z");
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
			auditor.AuditSend(mockEnvelope.Object, null);

		It should_append_all_client_ip_address_to_the_outgoing_headers = () =>
			messageHeaders["x-audit-client-ip"].ShouldEqual("1.1.1.1, 2.2.2.2, 3.3.3.3, 127.0.0.1");
	}

	[Subject(typeof(HttpRequestAuditor))]
	public class when_a_message_is_being_proxied_by_the_same_address_multiple_times : using_an_http_request_auditor
	{
		Establish context = () =>
		{
			mockRequest.Setup(x => x.UserHostAddress).Returns("127.0.0.1");
			requestHeaders["X-Forwarded-For"] = "127.0.0.1, 1.1.1.1, 2.2.2.2, 3.3.3.3";
		};

		Because of = () =>
			auditor.AuditSend(mockEnvelope.Object, null);

		It should_return_only_the_unique_values = () =>
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
			serverVariables = new NameValueCollection();
			mockRequest = new Mock<HttpRequestBase>();
			mockContext = new Mock<HttpContextBase>();
			requestHeaders = new NameValueCollection();
			mockContext.Setup(x => x.Request).Returns(mockRequest.Object);
			mockRequest.Setup(x => x.Headers).Returns(requestHeaders);
			mockRequest.Setup(x => x.ServerVariables).Returns(serverVariables);

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

		protected static NameValueCollection serverVariables;
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
#pragma warning restore 169, 414