namespace NanoMessageBus.Channels
{
	using System;
	using System.Linq;
	using System.Security.Cryptography.X509Certificates;

	public class CertificateStore
	{
		public virtual X509Certificate Resolve(string thumbprint, string name = "My", string location = "CurrentUser")
		{
			if (string.IsNullOrEmpty(thumbprint))
				return null;

			StoreName parsedName;
			if (!Enum.TryParse(name, true, out parsedName))
				return null;

			StoreLocation parsedLocation;
			if (!Enum.TryParse(location, true, out parsedLocation))
				return null;

			var source = new X509Store(parsedName, parsedLocation);

			try
			{
				source.Open(OpenFlags.ReadOnly);

				return source.Certificates
					.Find(X509FindType.FindByThumbprint, thumbprint, true)
					.OfType<X509Certificate>()
					.FirstOrDefault();
			}
			finally
			{
				source.Close();
			}
		}
	}
}