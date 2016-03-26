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
				throw new ArgumentNullException(nameof(context));

			return new HttpContextClone(context);
		}
	}

	internal class HttpContextClone : HttpContextBase
	{
		public override Exception Error
		{
			get { return this._error; }
		}
		public override bool IsCustomErrorEnabled
		{
			get { return this._customErrors; }
		}
		public override bool IsDebuggingEnabled
		{
			get { return this._debuggingEnabled; }
		}
		public override bool IsPostNotification
		{
			get { return this._postNotification; }
		}
		public override DateTime Timestamp
		{
			get { return this._timestamp; }
		}
		public override Exception[] AllErrors
		{
			get { return this._allErrors; }
		}
		public override bool SkipAuthorization
		{
			get { return this._skipAuthorization; }
			set { }
		}
		public override IPrincipal User
		{
			get { return this._user; }
			set { }
		}
		public override HttpRequestBase Request
		{
			get { return this._request; }
		}

		public HttpContextClone(HttpContextBase context)
		{
			this._error = context.Error;
			this._allErrors = context.AllErrors;
			this._debuggingEnabled = context.IsDebuggingEnabled;
			this._customErrors = context.IsCustomErrorEnabled;
			this._postNotification = IsMicrosoftRuntime && context.IsPostNotification;
			this._timestamp = context.Timestamp;
			this._skipAuthorization = context.SkipAuthorization;
			this._user = context.User;

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
		public override string[] AcceptTypes
		{
			get { return this._acceptTypes; }
		}
		public override string AnonymousID
		{
			get { return this._anonymousId; }
		}
		public override string ApplicationPath
		{
			get { return this._applicationPath; }
		}
		public override string AppRelativeCurrentExecutionFilePath
		{
			get { return this._appRelativeCurrentExecutionFilePath; }
		}
		public override Encoding ContentEncoding
		{
			get { return this._contentEncoding; }
			set { }
		}
		public override int ContentLength
		{
			get { return this._contentLength; }
		}
		public override string ContentType
		{
			get { return this._contentType; }
		}
		public override string CurrentExecutionFilePath
		{
			get { return this._currentExecutionFilePath; }
		}
		public override string HttpMethod
		{
			get { return this._httpMethod; }
		}
		public override bool IsAuthenticated
		{
			get { return this._isAuthenticated; }
		}
		public override bool IsLocal
		{
			get { return this._isLocal; }
		}
		public override bool IsSecureConnection
		{
			get { return this._isSecureConnection; }
		}
		public override string Path
		{
			get { return this._path; }
		}
		public override string PathInfo
		{
			get { return this._pathInfo; }
		}
		public override string PhysicalApplicationPath
		{
			get { return this._physicalApplicationPath; }
		}
		public override string PhysicalPath
		{
			get { return this._physicalPath; }
		}
		public override string RawUrl
		{
			get { return this._rawUrl; }
		}
		public override string RequestType
		{
			get { return this._requestType; }
		}
		public override int TotalBytes
		{
			get { return this._totalBytes; }
		}
		public override Uri Url
		{
			get { return this._url; }
		}
		public override Uri UrlReferrer
		{
			get { return this._urlReferrer; }
		}
		public override string UserHostAddress
		{
			get { return this._userHostAddress; }
		}
		public override string UserHostName
		{
			get { return this._userHostName; }
		}
		public override string UserAgent
		{
			get { return this._userAgent; }
		}
		public override string[] UserLanguages
		{
			get { return this._userLanguages; }
		}

		public override HttpCookieCollection Cookies
		{
			get { return this._cookies; }
		}
		public override NameValueCollection Headers
		{
			get { return this._headers; }
		}
		public override NameValueCollection Form
		{
			get { return this._form; }
		}
		public override NameValueCollection QueryString
		{
			get { return this._queryString; }
		}
		public override NameValueCollection ServerVariables
		{
			get { return this._serverVariables; }
		}

		public HttpRequestClone(HttpRequestBase request)
		{
			this._acceptTypes = request.AcceptTypes;
			this._anonymousId = request.AnonymousID;
			this._applicationPath = request.ApplicationPath;
			this._appRelativeCurrentExecutionFilePath = request.AppRelativeCurrentExecutionFilePath;
			this._contentEncoding = request.ContentEncoding;
			this._contentLength = request.ContentLength;
			this._contentType = request.ContentType;
			this._currentExecutionFilePath = request.CurrentExecutionFilePath;
			this._isSecureConnection = request.IsSecureConnection;
			this._isLocal = request.IsLocal;
			this._isAuthenticated = request.IsAuthenticated;
			this._httpMethod = request.HttpMethod;
			this._path = request.Path;
			this._pathInfo = request.PathInfo;
			this._physicalPath = request.PhysicalPath;
			this._physicalApplicationPath = request.PhysicalApplicationPath;
			this._rawUrl = request.RawUrl;
			this._requestType = request.RequestType;
			this._totalBytes = request.TotalBytes;
			this._url = request.Url;
			this._urlReferrer = request.UrlReferrer;
			this._userHostAddress = request.UserHostAddress;
			this._userHostName = request.UserHostName;
			this._userAgent = request.UserAgent;
			this._userLanguages = request.UserLanguages;

			this._headers = new NameValueCollection(request.Headers);
			this._form = new NameValueCollection(request.Form);
			this._queryString = new NameValueCollection(request.QueryString);
			this._serverVariables = new NameValueCollection(request.ServerVariables);

			this._cookies = new HttpCookieCollection();
			for (var i = 0; i < request.Cookies.Count; i++)
				this._cookies.Add(request.Cookies[i]);
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