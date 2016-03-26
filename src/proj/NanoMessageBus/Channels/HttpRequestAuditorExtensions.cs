namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections.Specialized;
	using System.Security.Principal;
	using System.Text;
	using System.Web;

	public static class HttpRequestAuditorExtensions
	{
		public static HttpContextBase Clone(this HttpContext context)
		{
			return context == null ? null : (new HttpContextWrapper(context)).Clone();
		}
		public static HttpContextBase Clone(this HttpContextBase context)
		{
			if (context == null)
			{
			    throw new ArgumentNullException(nameof(context));
			}

		    return new HttpContextClone(context);
		}
	}

	internal class HttpContextClone : HttpContextBase
	{
		public override Exception Error => _error;

	    public override bool IsCustomErrorEnabled => _customErrors;
	    public override bool IsDebuggingEnabled => _debuggingEnabled;

	    public override bool IsPostNotification => _postNotification;
	    public override DateTime Timestamp => _timestamp;

	    public override Exception[] AllErrors => _allErrors;

	    public override bool SkipAuthorization
		{
			get { return _skipAuthorization; }
			set { }
		}
		public override IPrincipal User
		{
			get { return _user; }
			set { }
		}
		public override HttpRequestBase Request => _request;

	    public HttpContextClone(HttpContextBase context)
		{
			_error = context.Error;
			_allErrors = context.AllErrors;
			_debuggingEnabled = context.IsDebuggingEnabled;
			_customErrors = context.IsCustomErrorEnabled;
			_postNotification = IsMicrosoftRuntime && context.IsPostNotification;
			_timestamp = context.Timestamp;
			_skipAuthorization = context.SkipAuthorization;
			_user = context.User;

			_request = new HttpRequestClone(context.Request);
		}

		private static readonly bool IsMicrosoftRuntime = Type.GetType("Mono.Runtime") == null;
		private readonly HttpRequestBase _request;
		private readonly Exception _error;
		private readonly Exception[] _allErrors;
		private readonly bool _postNotification;
		private readonly bool _debuggingEnabled;
		private readonly bool _customErrors;
		private readonly DateTime _timestamp;
		private readonly bool _skipAuthorization;
		private readonly IPrincipal _user;
	}

	internal class HttpRequestClone : HttpRequestBase
	{
		public override string[] AcceptTypes => _acceptTypes;

	    public override string AnonymousID => _anonymousId;

	    public override string ApplicationPath => _applicationPath;

	    public override string AppRelativeCurrentExecutionFilePath => _appRelativeCurrentExecutionFilePath;

	    public override Encoding ContentEncoding
		{
			get { return _contentEncoding; }
			set { }
		}
		public override int ContentLength => _contentLength;
	    public override string ContentType => _contentType;

	    public override string CurrentExecutionFilePath => _currentExecutionFilePath;

	    public override string HttpMethod => _httpMethod;

	    public override bool IsAuthenticated => _isAuthenticated;

	    public override bool IsLocal => _isLocal;

	    public override bool IsSecureConnection => _isSecureConnection;

	    public override string Path => _path;

	    public override string PathInfo => _pathInfo;

	    public override string PhysicalApplicationPath => _physicalApplicationPath;

	    public override string PhysicalPath => _physicalPath;

	    public override string RawUrl => _rawUrl;

	    public override string RequestType => _requestType;

	    public override int TotalBytes => _totalBytes;

	    public override Uri Url => _url;

	    public override Uri UrlReferrer => _urlReferrer;
	    public override string UserHostAddress => _userHostAddress;

	    public override string UserHostName => _userHostName;

	    public override string UserAgent => _userAgent;

	    public override string[] UserLanguages => _userLanguages;

	    public override HttpCookieCollection Cookies => _cookies;

	    public override NameValueCollection Headers => _headers;

	    public override NameValueCollection Form => _form;

	    public override NameValueCollection QueryString => _queryString;

	    public override NameValueCollection ServerVariables => _serverVariables;

	    public HttpRequestClone(HttpRequestBase request)
		{
			_acceptTypes = request.AcceptTypes;
			_anonymousId = request.AnonymousID;
			_applicationPath = request.ApplicationPath;
			_appRelativeCurrentExecutionFilePath = request.AppRelativeCurrentExecutionFilePath;
			_contentEncoding = request.ContentEncoding;
			_contentLength = request.ContentLength;
			_contentType = request.ContentType;
			_currentExecutionFilePath = request.CurrentExecutionFilePath;
			_isSecureConnection = request.IsSecureConnection;
			_isLocal = request.IsLocal;
			_isAuthenticated = request.IsAuthenticated;
			_httpMethod = request.HttpMethod;
			_path = request.Path;
			_pathInfo = request.PathInfo;
			_physicalPath = request.PhysicalPath;
			_physicalApplicationPath = request.PhysicalApplicationPath;
			_rawUrl = request.RawUrl;
			_requestType = request.RequestType;
			_totalBytes = request.TotalBytes;
			_url = request.Url;
			_urlReferrer = request.UrlReferrer;
			_userHostAddress = request.UserHostAddress;
			_userHostName = request.UserHostName;
			_userAgent = request.UserAgent;
			_userLanguages = request.UserLanguages;

			_headers = new NameValueCollection(request.Headers);
			_form = new NameValueCollection(request.Form);
			_queryString = new NameValueCollection(request.QueryString);
			_serverVariables = new NameValueCollection(request.ServerVariables);

			_cookies = new HttpCookieCollection();
			for (var i = 0; i < request.Cookies.Count; i++)
			{
			    _cookies.Add(request.Cookies[i]);
			}
		}

		private readonly string _userAgent;
		private readonly string[] _acceptTypes;
		private readonly string _anonymousId;
		private readonly string _applicationPath;
		private readonly string _appRelativeCurrentExecutionFilePath;
		private readonly Encoding _contentEncoding;
		private readonly int _contentLength;
		private readonly string _contentType;
		private readonly string _currentExecutionFilePath;
		private readonly bool _isSecureConnection;
		private readonly bool _isLocal;
		private readonly bool _isAuthenticated;
		private readonly string _httpMethod;
		private readonly string _path;
		private readonly string _pathInfo;
		private readonly string _physicalApplicationPath;
		private readonly string _physicalPath;
		private readonly string _rawUrl;
		private readonly string _requestType;
		private readonly int _totalBytes;
		private readonly Uri _url;
		private readonly Uri _urlReferrer;
		private readonly string _userHostAddress;
		private readonly string _userHostName;
		private readonly string[] _userLanguages;
		private readonly HttpCookieCollection _cookies;
		private readonly NameValueCollection _headers;
		private readonly NameValueCollection _queryString;
		private readonly NameValueCollection _form;
		private readonly NameValueCollection _serverVariables;
	}
}