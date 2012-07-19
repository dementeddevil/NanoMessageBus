#pragma warning disable 169
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
			thrown.ShouldBeOfType<ArgumentNullException>();

		static Exception thrown;
	}

	[Subject(typeof(HttpRequestAuditorExtensions))]
	public class when_cloning_a_null_HttpContext
	{
		Because of = () =>
			clone = HttpContext.Current.Clone();

		It should_return_null = () =>
			clone.ShouldBeNull();

		static HttpContextBase clone;
	}

	[Subject(typeof(HttpRequestAuditorExtensions))]
	public class when_cloning_an_http_context : using_an_http_context
	{
		Because of = () =>
			clone = original.Clone();

		It should_return_a_object = () =>
			clone.ShouldNotBeNull();

		It should_not_return_the_same_instance_as_provided = () =>
			ReferenceEquals(clone, original).ShouldBeFalse();

		It should_populate_the_request = () =>
			clone.Request.ShouldNotBeNull();

		It should_not_return_the_same_instance_of_the_request_provided = () =>
			ReferenceEquals(clone.Request, original.Request).ShouldBeFalse();

		It should_return_a_reference_to_any_error_on_the_original = () =>
			clone.Error.ShouldEqual(original.Error);

		It should_return_a_reference_to_all_on_the_original = () =>
			(clone.AllErrors ?? new Exception[0]).SequenceEqual(
				original.AllErrors ?? new Exception[0]).ShouldBeTrue();

		It should_indicate_if_debugging_is_enabled = () =>
			clone.IsDebuggingEnabled.ShouldEqual(original.IsDebuggingEnabled);

		It should_indicate_if_custom_errors_are_enabled = () =>
			clone.IsCustomErrorEnabled.ShouldEqual(original.IsCustomErrorEnabled);

		It should_indicate_if_the_request_is_a_post = () =>
			clone.IsPostNotification.ShouldEqual(original.IsPostNotification);

		It should_indicate_if_authorization_has_been_skipped = () =>
			clone.SkipAuthorization.ShouldEqual(original.SkipAuthorization);

		It should_clone_the_context_timestamp = () =>
			clone.Timestamp.ShouldEqual(original.Timestamp);

		It should_return_a_reference_to_the_current_principal = () =>
			clone.User.ShouldEqual(original.User);

		It should_return_the_same_user_agent = () =>
			clone.Request.UserAgent.ShouldEqual(original.Request.UserAgent);

		It should_return_the_same_accept_types = () =>
			clone.Request.AcceptTypes.ShouldEqual(original.Request.AcceptTypes);

		It should_return_the_same_anonymous_id = () =>
			clone.Request.AnonymousID.ShouldEqual(original.Request.AnonymousID);

		It should_return_the_same_app_path = () =>
			clone.Request.ApplicationPath.ShouldEqual(original.Request.ApplicationPath);

		It should_return_the_same_app_relative_path = () =>
			clone.Request.AppRelativeCurrentExecutionFilePath.ShouldEqual(
				original.Request.AppRelativeCurrentExecutionFilePath);

		It should_return_the_same_content_encoding = () =>
			clone.Request.ContentEncoding.ShouldEqual(original.Request.ContentEncoding);

		It should_return_the_same_content_length = () =>
			clone.Request.ContentLength.ShouldEqual(original.Request.ContentLength);

		It should_return_the_same_content_type = () =>
			clone.Request.ContentType.ShouldEqual(original.Request.ContentType);

		It should_return_the_same_execution_path = () =>
			clone.Request.CurrentExecutionFilePath.ShouldEqual(original.Request.CurrentExecutionFilePath);

		It should_return_the_same_http_method = () =>
			clone.Request.HttpMethod.ShouldEqual(original.Request.HttpMethod);

		It should_return_the_same_is_authenticated_property = () =>
			clone.Request.IsAuthenticated.ShouldEqual(original.Request.IsAuthenticated);

		It should_return_the_same_is_local_property = () =>
			clone.Request.IsLocal.ShouldEqual(original.Request.IsLocal);

		It should_return_the_same_is_secure_connection_property = () =>
			clone.Request.IsSecureConnection.ShouldEqual(original.Request.IsSecureConnection);

		It should_return_the_same_path = () =>
			clone.Request.Path.ShouldEqual(original.Request.Path);

		It should_return_the_same_path_info = () =>
			clone.Request.PathInfo.ShouldEqual(original.Request.PathInfo);

		It should_return_the_same_physical_app_path = () =>
			clone.Request.PhysicalApplicationPath.ShouldEqual(original.Request.PhysicalApplicationPath);

		It should_return_the_same_physical_path = () =>
			clone.Request.PhysicalPath.ShouldEqual(original.Request.PhysicalPath);

		It should_return_the_same_raw_url = () =>
			clone.Request.RawUrl.ShouldEqual(original.Request.RawUrl);

		It should_return_the_same_request_type = () =>
			clone.Request.RequestType.ShouldEqual(original.Request.RequestType);

		It should_return_the_same_total_bytes_value = () =>
			clone.Request.TotalBytes.ShouldEqual(original.Request.TotalBytes);

		It should_return_the_same_url = () =>
			clone.Request.Url.ShouldEqual(original.Request.Url);

		It should_return_the_same_referring_url = () =>
			clone.Request.UrlReferrer.ShouldEqual(original.Request.UrlReferrer);

		It should_return_the_same_user_host_address = () =>
			clone.Request.UserHostAddress.ShouldEqual(original.Request.UserHostAddress);

		It should_return_the_same_user_host_name = () =>
			clone.Request.UserHostName.ShouldEqual(original.Request.UserHostName);

		It should_return_the_same_user_languages = () =>
			clone.Request.UserLanguages.ShouldEqual(original.Request.UserLanguages);

		It should_return_the_cookie_collection = () =>
			clone.Request.Cookies.ShouldNotBeNull();

		It should_return_the_clone_the_cookies = () =>
			ReferenceEquals(clone.Request.Cookies, original.Request.Cookies).ShouldBeFalse();

		It should_contain_all_request_cookies = () =>
			clone.Request.Cookies.Count.ShouldEqual(original.Request.Cookies.Count);

		It should_return_the_header_collection = () =>
			clone.Request.Headers.ShouldNotBeNull();

		It should_return_the_clone_the_headers = () =>
			ReferenceEquals(clone.Request.Headers, original.Request.Headers).ShouldBeFalse();

		It should_contain_all_request_headers= () =>
			clone.Request.Headers.Count.ShouldEqual(original.Request.Headers.Count);

		It should_return_the_form_collection = () =>
			clone.Request.Form.ShouldNotBeNull();

		It should_return_the_clone_the_form_data = () =>
			ReferenceEquals(clone.Request.Form, original.Request.Form).ShouldBeFalse();

		It should_contain_all_request_form_data = () =>
			clone.Request.Form.Count.ShouldEqual(original.Request.Form.Count);

		It should_return_the_query_string_collection = () =>
			clone.Request.QueryString.ShouldNotBeNull();

		It should_return_the_clone_the_query_string_data = () =>
			ReferenceEquals(clone.Request.QueryString, original.Request.QueryString).ShouldBeFalse();

		It should_contain_all_request_query_string_data = () =>
			clone.Request.QueryString.Count.ShouldEqual(original.Request.QueryString.Count);

		It should_return_the_server_variables_collection = () =>
			clone.Request.ServerVariables.ShouldNotBeNull();

		It should_return_the_clone_the_server_variables_data = () =>
			ReferenceEquals(clone.Request.ServerVariables, original.Request.ServerVariables).ShouldBeFalse();

		It should_contain_all_request_server_variables_data = () =>
			clone.Request.ServerVariables.Count.ShouldEqual(original.Request.ServerVariables.Count);

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
			clone.SkipAuthorization.ShouldBeTrue();

		It should_NOT_set_user_property = () =>
			clone.User.ShouldEqual(original.User);

		It should_NOT_set_the_content_encoding = () =>
			clone.Request.ContentEncoding.ShouldEqual(original.Request.ContentEncoding);
		
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
#pragma warning restore 169