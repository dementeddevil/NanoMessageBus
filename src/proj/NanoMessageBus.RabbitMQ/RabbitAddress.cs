namespace NanoMessageBus.RabbitMQ
{
	using System;

	public class RabbitAddress
	{
		public string UserName { get; private set; }
		public string Password { get; private set; }
		public string Host { get; private set; }
		public string VirtualHost { get; set; } // TODO: where does this come from?
		public string Exchange { get; private set; }
		public string Queue { get; private set; }
		public Uri Raw { get; private set; }

		public RabbitAddress(string address)
			: this(new Uri(address))
		{
		}
		public RabbitAddress(Uri address)
		{
			this.Raw = address;

			var auth = address.UserInfo.Split(new[] { '@' }); // TODO: user/pass is URL encoded
			if (auth.Length > 0)
				this.UserName = auth[0];
			if (auth.Length > 1)
				this.Password = auth[1];

			this.Host = address.Host;

			if (!string.IsNullOrEmpty(address.AbsolutePath))
				this.Exchange = address.AbsolutePath.Substring(1); // remove the leading slash;

			if (!string.IsNullOrEmpty(address.Query))
				this.Queue = address.Query.Substring(1); // remove the leading ? character
		}
	}
}