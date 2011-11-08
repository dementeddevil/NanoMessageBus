namespace NanoMessageBus.SubscriptionStorage.Raven
{
	using System;

	public class Subscription
	{
		public Subscription(string subscriber, string messageType, DateTime? expiration)
		{
			if (subscriber == null)
				throw new ArgumentNullException("subscriber");

			if (messageType == null)
				throw new ArgumentNullException("messageType");

			this.Subscriber = subscriber;
			this.MessageType = messageType;
			this.Expiration = expiration;
		}

		public string Id { get; set; } // set by RavenDB
		public string Subscriber { get; private set; }
		public string MessageType { get; set; }
		public DateTime? Expiration { get; set; }
	}
}