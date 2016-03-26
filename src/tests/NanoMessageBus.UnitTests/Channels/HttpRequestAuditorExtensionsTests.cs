using FluentAssertions;

#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections.Specialized;
	using System.Globalization;
	using System.Linq;
	using System.Security.Principal;
	using System.Text;
	using System.Web;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject(typeof(HttpRequestAuditorExtensions))]
	public class when_cloning_a_null_HttpContextBase
	{
		private Because of = () =>
			thrown = Catch.Exception(() => ((HttpContextBase)null).Clone());

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ArgumentNullException>();

		static Exception thrown;
	}

	[Subject(typeof(HttpRequestAuditorExtensions))]
	public class when_cloning_a_null_HttpContext
	{
		Because of = () =>
			clone = HttpContext.Current.Clone();

		It should_return_null = () =>
			clone.Should().BeNull();

		static HttpContextBase clone;
	}

	[Subject(typeof(HttpRequestAuditorExtensions))]
	public class when_cloning_an_http_context : using_an_http_context
	{
		Because of = () =>
			clone = original.Clone();

		It should_return_a_object = () =>
			clone.Should().NotBeNull();

		It should_not_return_the_same_instance_as_provided = () =>
			ReferenceEquals(clone, original).Should().BeFalse();

		It should_populate_the_request = () =>
			clone.Request.Should().NotBeNull();

		It should_not_return_the_same_instance_of_the_request_provided = () =>
			ReferenceEquals(clone.Request, original.Request).Should().BeFalse();

		It should_return_a_reference_to_any_error_on_the_original = () =>
			clone.Error.Should().Be(original.Error);

		It should_return_a_reference_to_all_on_the_original = () =>
			(clone.AllErrors ?? new Exception[0]).SequenceEqual(
				original.AllErrors ?? new Exception[0]).Should().BeTrue();

		It should_indicate_if_debugging_is_enabled = () =>
			clone.IsDebuggingEnabled.Should().Be(original.IsDebuggingEnabled);

		It should_indicate_if_custom_errors_are_enabled = () =>
			clone.IsCustomErrorEnabled.Should().Be(original.IsCustomErrorEnabled);

		It should_indicate_if_the_request_is_a_post = () =>
			clone.IsPostNotification.Should().Be(original.IsPostNotification);

		It should_indicate_if_authorization_has_been_skipped = () =>
			clone.SkipAuthorization.Should().Be(original.SkipAuthorization);

		It should_clone_the_context_timestamp = () =>
			clone.Timestamp.Should().Be(original.Timestamp);

		It should_return_a_reference_to_the_current_principal = () =>
			clone.User.Should().Be(original.User);

		It should_return_the_same_user_agent = () =>
			clone.Request.UserAgent.Should().Be(original.Request.UserAgent);

		It should_return_the_same_accept_types = () =>
			clone.Request.AcceptTypes.Should().BeSameAs(original.Request.AcceptTypes);

		It should_return_the_same_anonymous_id = () =>
			clone.Request.AnonymousID.Should().Be(original.Request.AnonymousID);

		It should_return_the_same_app_path = () =>
			clone.Request.ApplicationPath.Should().Be(original.Request.ApplicationPath);

		It should_return_the_same_app_relative_path = () =>
			clone.Request.AppRelativeCurrentExecutionFilePath.Should().Be(
				original.Request.AppRelativeCurrentExecutionFilePath);

		It should_return_the_same_content_encoding = () =>
			clone.Request.ContentEncoding.Should().Be(original.Request.ContentEncoding);

		It should_return_the_same_content_length = () =>
			clone.Request.ContentLength.Should().Be(original.Request.ContentLength);

		It should_return_the_same_content_type = () =>
			clone.Request.ContentType.Should().Be(original.Request.ContentType);

		It should_return_the_same_execution_path = () =>
			clone.Request.CurrentExecutionFilePath.Should().Be(original.Request.CurrentExecutionFilePath);

		It should_return_the_same_http_method = () =>
			clone.Request.HttpMethod.Should().Be(original.Request.HttpMethod);

		It should_return_the_same_is_authenticated_property = () =>
			clone.Request.IsAuthenticated.Should().Be(original.Request.IsAuthenticated);

		It should_return_the_same_is_local_property = () =>
			clone.Request.IsLocal.Should().Be(original.Request.IsLocal);

		It should_return_the_same_is_secure_connection_property = () =>
			clone.Request.IsSecureConnection.Should().Be(original.Request.IsSecureConnection);

		It should_return_the_same_path = () =>
			clone.Request.Path.Should().Be(original.Request.Path);

		It should_return_the_same_path_info = () =>
			clone.Request.PathInfo.Should().Be(original.Request.PathInfo);

		It should_return_the_same_physical_app_path = () =>
			clone.Request.PhysicalApplicationPath.Should().Be(original.Request.PhysicalApplicationPath);

		It should_return_the_same_physical_path = () =>
			clone.Request.PhysicalPath.Should().Be(original.Request.PhysicalPath);

		It should_return_the_same_raw_url = () =>
			clone.Request.RawUrl.Should().Be(original.Request.RawUrl);

		It should_return_the_same_request_type = () =>
			clone.Request.RequestType.Should().Be(original.Request.RequestType);

		It should_return_the_same_total_bytes_value = () =>
			clone.Request.TotalBytes.Should().Be(original.Request.TotalBytes);

		It should_return_the_same_url = () =>
			clone.Request.Url.Should().Be(original.Request.Url);

		It should_return_the_same_referring_url = () =>
			clone.Request.UrlReferrer.Should().Be(original.Request.UrlReferrer);

		It should_return_the_same_user_host_address = () =>
			clone.Request.UserHostAddress.Should().Be(original.Request.UserHostAddress);

		It should_return_the_same_user_host_name = () =>
			clone.Request.UserHostName.Should().Be(original.Request.UserHostName);

		It should_return_the_same_user_languages = () =>
			clone.Request.UserLanguages.Should().BeSameAs(original.Request.UserLanguages);

		It should_return_the_cookie_collection = () =>
			clone.Request.Cookies.Should().NotBeNull();

		It should_return_the_clone_the_cookies = () =>
			ReferenceEquals(clone.Request.Cookies, original.Request.Cookies).Should().BeFalse();

		It should_contain_all_request_cookies = () =>
			clone.Request.Cookies.Count.Should().Be(original.Request.Cookies.Count);

		It should_return_the_header_collection = () =>
			clone.Request.Headers.Should().NotBeNull();

		It should_return_the_clone_the_headers = () =>
			ReferenceEquals(clone.Request.Headers, original.Request.Headers).Should().BeFalse();

		It should_contain_all_request_headers= () =>
			clone.Request.Headers.Count.Should().Be(original.Request.Headers.Count);

		It should_return_the_form_collection = () =>
			clone.Request.Form.Should().NotBeNull();

		It should_return_the_clone_the_form_data = () =>
			ReferenceEquals(clone.Request.Form, original.Request.Form).Should().BeFalse();

		It should_contain_all_request_form_data = () =>
			clone.Request.Form.Count.Should().Be(original.Request.Form.Count);

		It should_return_the_query_string_collection = () =>
			clone.Request.QueryString.Should().NotBeNull();

		It should_return_the_clone_the_query_string_data = () =>
			ReferenceEquals(clone.Request.QueryString, original.Request.QueryString).Should().BeFalse();

		It should_contain_all_request_query_string_data = () =>
			clone.Request.QueryString.Count.Should().Be(original.Request.QueryString.Count);

		It should_return_the_server_variables_collection = () =>
			clone.Request.ServerVariables.Should().NotBeNull();

		It should_return_the_clone_the_server_variables_data = () =>
			ReferenceEquals(clone.Request.ServerVariables, original.Request.ServerVariables).Should().BeFalse();

		It should_contain_all_request_server_variables_data = () =>
			clone.Request.ServerVariables.Count.Should().Be(original.Request.ServerVariables.Count);

		static HttpContextBase clone;
	}

	[Subject(typeof(HttpRequestAuditorExtensions))]
	public class when_assigning_settable_values : using_an_http_context
	{
		Establish context = () =>
			clone = original.Clone();

		Because of = () =>
		{
			clone.SkipAuthorization = false;
			clone.User = null;
			clone.Request.ContentEncoding = Encoding.UTF7;
		};

		It should_NOT_set_the_skip_authorization_value = () =>
			clone.SkipAuthorization.Should().BeTrue();

		It should_NOT_set_user_property = () =>
			clone.User.Should().Be(original.User);

		It should_NOT_set_the_content_encoding = () =>
			clone.Request.ContentEncoding.Should().Be(original.Request.ContentEncoding);
		
		static HttpContextBase clone;
	}

	public abstract class using_an_http_context
	{
		Establish context = () =>
		{
			var mockContext = new Mock<HttpContextBase>();
			var mockRequest = new Mock<HttpRequestBase>();

			mockContext.Setup(x => x.Request).Returns(mockRequest.Object);
			mockContext.Setup(x => x.User).Returns(new Mock<IPrincipal>().Object);

			mockContext.Setup(x => x.Error).Returns(error);
			mockContext.Setup(x => x.AllErrors).Returns(new[] { error });
			mockContext.Setup(x => x.IsDebuggingEnabled).Returns(true);
			mockContext.Setup(x => x.IsCustomErrorEnabled).Returns(true);
			mockContext.Setup(x => x.IsPostNotification).Returns(MicrosoftRuntime); // not supported on Mono
			mockContext.Setup(x => x.SkipAuthorization).Returns(true);
			mockContext.Setup(x => x.Timestamp).Returns(SystemTime.UtcNow);

			mockRequest.Setup(x => x.UserAgent).Returns("user agent");
			mockRequest.Setup(x => x.AcceptTypes).Returns(new[] { "gzip" });
			mockRequest.Setup(x => x.AnonymousID).Returns("anonymous id");
			mockRequest.Setup(x => x.ApplicationPath).Returns("application path");
			mockRequest.Setup(x => x.AppRelativeCurrentExecutionFilePath).Returns("app relative path");
			mockRequest.Setup(x => x.ContentEncoding).Returns(Encoding.UTF32);
			mockRequest.Setup(x => x.ContentLength).Returns(42);
			mockRequest.Setup(x => x.ContentType).Returns("custom/content");
			mockRequest.Setup(x => x.FilePath).Returns("file/path/here");
			mockRequest.Setup(x => x.HttpMethod).Returns("custom method");
			mockRequest.Setup(x => x.IsAuthenticated).Returns(true);
			mockRequest.Setup(x => x.IsLocal).Returns(true);
			mockRequest.Setup(x => x.IsSecureConnection).Returns(true);
			mockRequest.Setup(x => x.Path).Returns("standard/path");
			mockRequest.Setup(x => x.PathInfo).Returns("standard/path/info");
			mockRequest.Setup(x => x.PhysicalApplicationPath).Returns("physical app path");
			mockRequest.Setup(x => x.PhysicalPath).Returns("physical/path");
			mockRequest.Setup(x => x.RawUrl).Returns("raw/url?value");
			mockRequest.Setup(x => x.RequestType).Returns("request-type");
			mockRequest.Setup(x => x.TotalBytes).Returns(42 + 42);
			mockRequest.Setup(x => x.Url).Returns(new Uri("http://www.google.com/"));
			mockRequest.Setup(x => x.UrlReferrer).Returns(new Uri("http://www.google.com/referer"));
			mockRequest.Setup(x => x.UserHostAddress).Returns("host-address");
			mockRequest.Setup(x => x.UserHostName).Returns("host-name");
			mockRequest.Setup(x => x.UserLanguages).Returns(new[] { "en-us" });

			mockRequest.Setup(x => x.Headers).Returns(Generate(1));
			mockRequest.Setup(x => x.Form).Returns(Generate(2));
			mockRequest.Setup(x => x.QueryString).Returns(Generate(3));
			mockRequest.Setup(x => x.ServerVariables).Returns(Generate(4));
			mockRequest.Setup(x => x.Cookies).Returns(new HttpCookieCollection
			{
				new HttpCookie("key", "value")
			});

			original = mockContext.Object;
		};

		private static NameValueCollection Generate(int items)
		{
			var collection = new NameValueCollection();

			for (var i = 0; i < items; i++)
				collection[i.ToString(CultureInfo.InvariantCulture)] = Guid.NewGuid().ToString();

			return collection;
		}

		private static readonly bool MicrosoftRuntime = Type.GetType("Mono.Runtime") == null;
		protected static HttpContextBase original;
		static readonly Exception error = new Exception();
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169, 414