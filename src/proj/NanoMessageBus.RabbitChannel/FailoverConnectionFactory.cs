namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.Linq;
	using System.Net.Security;
	using System.Security.Authentication;
	using System.Security.Cryptography.X509Certificates;
	using System.Web;
	using RabbitMQ.Client;
	using RabbitMQ.Client.Exceptions;

	public class FailoverConnectionFactory : ConnectionFactory
	{
		public virtual IEnumerable<Uri> Endpoints
		{
			get { return this.brokers; }
		}

		public virtual FailoverConnectionFactory AddEndpoints(string endpoint)
		{
			var split = endpoint
				.Split(EndpointDelimiter, StringSplitOptions.RemoveEmptyEntries)
				.Select(x => new Uri(x, UriKind.Absolute));

			if (!string.IsNullOrEmpty(endpoint))
				this.AddEndpoints(split);

			return this;
		}
		public virtual FailoverConnectionFactory AddEndpoints(params Uri[] endpoints)
		{
			return this.AddEndpoints((IEnumerable<Uri>)endpoints);
		}
		public virtual FailoverConnectionFactory AddEndpoints(IEnumerable<Uri> endpoints)
		{
			(endpoints ?? new Uri[0])
				.Where(x => x != null)
				.ToList()
				.ForEach(x => this.AddEndpoint(x));

			return this;
		}
		public virtual bool AddEndpoint(Uri endpoint)
		{
			if (endpoint == null)
				throw new ArgumentNullException("endpoint");

			if (this.brokers.Contains(endpoint))
				return false;

			this.brokers.Add(endpoint);
			if (this.UserName != DefaultUserName || this.Password != DefaultPassword)
				return true;

			var authentication = endpoint.UserInfo.Split(AuthenticationDelimiter);
			this.UserName = authentication.Length > 0 ? authentication[UserNameIndex] : null;
			this.Password = authentication.Length > 1 ? authentication[PasswordIndex] : null;

			return true;
		}
		public virtual FailoverConnectionFactory RandomizeEndpoints()
		{
			var random = new Random();

			for (var i = this.brokers.Count - 1; i > 1; i--)
			{
				var next = random.Next(i + 1);
				var item = this.brokers[next];
				this.brokers[next] = this.brokers[i];
				this.brokers[i] = item;
			}

			return this;
		}

		public override IConnection CreateConnection(int maxRedirects)
		{
			this.RequestedHeartbeat = RequestedHeartbeatInSeconds;
			this.RequestedConnectionTimeout = 10 * 1000; // 10 seconds

			var endpoints = this.brokers
				.Select(x => new AmqpTcpEndpoint(x) { Ssl = this.ToSsl(x) })
				.ToArray();
			if (endpoints.Length == 0)
				endpoints = new[] { new AmqpTcpEndpoint(DefaultEndpoint) };

			var attempts = new Dictionary<AmqpTcpEndpoint, int>();
			var errors = new Dictionary<AmqpTcpEndpoint, Exception>();
			var connection = this.CreateConnection(maxRedirects, attempts, errors, endpoints);
			if (connection == null)
				throw new BrokerUnreachableException(attempts, errors, null);

			return connection;
		}
		private SslOption ToSsl(Uri address)
		{
			var parsed = HttpUtility.ParseQueryString(address.Query);
			var certificatePath = parsed[CertificatePathKey];
			var certificate = this.certificates.Resolve(parsed[CertificateIdKey]);
			var secureScheme = SecureScheme.Equals(address.Scheme, StringComparison.InvariantCultureIgnoreCase);
			var enabled = secureScheme || certificate != null || !string.IsNullOrEmpty(certificatePath);

			return new SslOption
			{
				Version = SslProtocols.Tls,
				AcceptablePolicyErrors = GetAcceptablePolicyFailures(parsed),
				ServerName = parsed[RemoteNameKey] ?? address.Host,
				Enabled = enabled,
				CertPath = certificatePath,
				CertPassphrase = parsed[CertificatePassphraseKey],
				Certs = certificate == null ? new X509CertificateCollection() : new X509CertificateCollection(new[] { certificate })
			};
		}
		private static SslPolicyErrors GetAcceptablePolicyFailures(NameValueCollection values)
		{
			var errors = SslPolicyErrors.None;
			if (values.AllKeys.Contains(IgnoreCertificateName))
				errors = errors | SslPolicyErrors.RemoteCertificateNameMismatch;

			if (values.AllKeys.Contains(IgnoreCertificateIssuer))
				errors = errors | SslPolicyErrors.RemoteCertificateChainErrors;

			return errors;
		}

		public FailoverConnectionFactory() : this(null)
		{
		}
		public FailoverConnectionFactory(CertificateStore certificates)
		{
			this.certificates = certificates ?? new CertificateStore();
		}

		private const string SecureScheme = "amqps";
		private const string CertificatePathKey = "cert-path";
		private const string RemoteNameKey = "remote-name";
		private const string CertificatePassphraseKey = "cert-passphrase";
		private const string IgnoreCertificateName = "ignore-name";
		private const string IgnoreCertificateIssuer = "ignore-issuer";
		private const string CertificateIdKey = "cert-id";
		private const string DefaultUserName = "guest";
		private const string DefaultPassword = DefaultUserName;
		private const int UserNameIndex = 0;
		private const int PasswordIndex = 1;
		private const int RequestedHeartbeatInSeconds = 15;
		private static readonly Uri DefaultEndpoint = new Uri("amqp://guest:guest@localhost:5672/");
		private static readonly char[] EndpointDelimiter = new[] { '|' };
		private static readonly char[] AuthenticationDelimiter = ":".ToCharArray();
		private readonly IList<Uri> brokers = new List<Uri>();
		private readonly CertificateStore certificates;
	}
}