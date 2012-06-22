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
				throw new ArgumentNullException("context");

			return new HttpContextClone(context);
		}
	}

	internal class HttpContextClone : HttpContextBase
	{
		public override Exception Error
		{
			get { return this.error; }
		}
		public override bool IsCustomErrorEnabled
		{
			get { return this.customErrors; }
		}
		public override bool IsDebuggingEnabled
		{
			get { return this.debuggingEnabled; }
		}
		public override bool IsPostNotification
		{
			get { return this.postNotification; }
		}
		public override DateTime Timestamp
		{
			get { return this.timestamp; }
		}
		public override Exception[] AllErrors
		{
			get { return this.allErrors; }
		}
		public override bool SkipAuthorization
		{
			get { return this.skipAuthorization; }
			set { }
		}
		public override IPrincipal User
		{
			get { return this.user; }
			set { }
		}
		public override HttpRequestBase Request
		{
			get { return this.request; }
		}

		public HttpContextClone(HttpContextBase context)
		{
			this.error = context.Error;
			this.allErrors = context.AllErrors;
			this.debuggingEnabled = context.IsDebuggingEnabled;
			this.customErrors = context.IsCustomErrorEnabled;
			this.postNotification = context.IsPostNotification;
			this.timestamp = context.Timestamp;
			this.skipAuthorization = context.SkipAuthorization;
			this.user = context.User;

			request = new HttpRequestClone(context.Request);
		}

		private readonly HttpRequestBase request;
		private readonly Exception error;
		private readonly Exception[] allErrors;
		private readonly bool postNotification;
		private readonly bool debuggingEnabled;
		private readonly bool customErrors;
		private readonly DateTime timestamp;
		private readonly bool skipAuthorization;
		private readonly IPrincipal user;
	}

	internal class HttpRequestClone : HttpRequestBase
	{
		public override string[] AcceptTypes
		{
			get { return this.acceptTypes; }
		}
		public override string AnonymousID
		{
			get { return this.anonymousId; }
		}
		public override string ApplicationPath
		{
			get { return this.applicationPath; }
		}
		public override string AppRelativeCurrentExecutionFilePath
		{
			get { return this.appRelativeCurrentExecutionFilePath; }
		}
		public override Encoding ContentEncoding
		{
			get { return this.contentEncoding; }
			set { }
		}
		public override int ContentLength
		{
			get { return this.contentLength; }
		}
		public override string ContentType
		{
			get { return this.contentType; }
		}
		public override string CurrentExecutionFilePath
		{
			get { return this.currentExecutionFilePath; }
		}
		public override string HttpMethod
		{
			get { return this.httpMethod; }
		}
		public override bool IsAuthenticated
		{
			get { return this.isAuthenticated; }
		}
		public override bool IsLocal
		{
			get { return this.isLocal; }
		}
		public override bool IsSecureConnection
		{
			get { return this.isSecureConnection; }
		}
		public override string Path
		{
			get { return this.path; }
		}
		public override string PathInfo
		{
			get { return this.pathInfo; }
		}
		public override string PhysicalApplicationPath
		{
			get { return this.physicalApplicationPath; }
		}
		public override string PhysicalPath
		{
			get { return this.physicalPath; }
		}
		public override string RawUrl
		{
			get { return this.rawUrl; }
		}
		public override string RequestType
		{
			get { return this.requestType; }
		}
		public override int TotalBytes
		{
			get { return this.totalBytes; }
		}
		public override Uri Url
		{
			get { return this.url; }
		}
		public override Uri UrlReferrer
		{
			get { return this.urlReferrer; }
		}
		public override string UserHostAddress
		{
			get { return this.userHostAddress; }
		}
		public override string UserHostName
		{
			get { return this.userHostName; }
		}
		public override string UserAgent
		{
			get { return this.userAgent; }
		}
		public override string[] UserLanguages
		{
			get { return this.userLanguages; }
		}

		public override HttpCookieCollection Cookies
		{
			get { return this.cookies; }
		}
		public override NameValueCollection Headers
		{
			get { return this.headers; }
		}
		public override NameValueCollection Form
		{
			get { return this.form; }
		}
		public override NameValueCollection QueryString
		{
			get { return this.queryString; }
		}
		public override NameValueCollection ServerVariables
		{
			get { return this.serverVariables; }
		}

		public HttpRequestClone(HttpRequestBase request)
		{
			this.acceptTypes = request.AcceptTypes;
			this.anonymousId = request.AnonymousID;
			this.applicationPath = request.ApplicationPath;
			this.appRelativeCurrentExecutionFilePath = request.AppRelativeCurrentExecutionFilePath;
			this.contentEncoding = request.ContentEncoding;
			this.contentLength = request.ContentLength;
			this.contentType = request.ContentType;
			this.currentExecutionFilePath = request.CurrentExecutionFilePath;
			this.isSecureConnection = request.IsSecureConnection;
			this.isLocal = request.IsLocal;
			this.isAuthenticated = request.IsAuthenticated;
			this.httpMethod = request.HttpMethod;
			this.path = request.Path;
			this.pathInfo = request.PathInfo;
			this.physicalPath = request.PhysicalPath;
			this.physicalApplicationPath = request.PhysicalApplicationPath;
			this.rawUrl = request.RawUrl;
			this.requestType = request.RequestType;
			this.totalBytes = request.TotalBytes;
			this.url = request.Url;
			this.urlReferrer = request.UrlReferrer;
			this.userHostAddress = request.UserHostAddress;
			this.userHostName = request.UserHostName;
			this.userAgent = request.UserAgent;
			this.userLanguages = request.UserLanguages;

			this.headers = new NameValueCollection(request.Headers);
			this.form = new NameValueCollection(request.Form);
			this.queryString = new NameValueCollection(request.QueryString);
			this.serverVariables = new NameValueCollection(request.ServerVariables);

			this.cookies = new HttpCookieCollection();
			for (var i = 0; i < request.Cookies.Count; i++)
				this.cookies.Add(request.Cookies[i]);
		}

		private readonly string userAgent;
		private readonly string[] acceptTypes;
		private readonly string anonymousId;
		private readonly string applicationPath;
		private readonly string appRelativeCurrentExecutionFilePath;
		private readonly Encoding contentEncoding;
		private readonly int contentLength;
		private readonly string contentType;
		private readonly string currentExecutionFilePath;
		private readonly bool isSecureConnection;
		private readonly bool isLocal;
		private readonly bool isAuthenticated;
		private readonly string httpMethod;
		private readonly string path;
		private readonly string pathInfo;
		private readonly string physicalApplicationPath;
		private readonly string physicalPath;
		private readonly string rawUrl;
		private readonly string requestType;
		private readonly int totalBytes;
		private readonly Uri url;
		private readonly Uri urlReferrer;
		private readonly string userHostAddress;
		private readonly string userHostName;
		private readonly string[] userLanguages;
		private readonly HttpCookieCollection cookies;
		private readonly NameValueCollection headers;
		private readonly NameValueCollection queryString;
		private readonly NameValueCollection form;
		private readonly NameValueCollection serverVariables;
	}
}