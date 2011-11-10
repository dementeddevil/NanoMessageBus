namespace NanoMessageBus.RabbitMQ
{
	using System;
	using System.Collections.Generic;
	using SubscriptionStorage;

	public class RabbitSubscriptionStorage : IStoreSubscriptions
	{
		public ICollection<Uri> GetSubscribers(IEnumerable<string> messageTypes)
		{
			// TODO: determine the exchange to which the messages should be dispatched
			// analyze metadata on the fly and/or lookup in dictionary provided by wireup?
			return null;
		}

		public void Subscribe(Uri address, IEnumerable<string> messageTypes, DateTime? expiration)
		{
		}
		public void Unsubscribe(Uri address, IEnumerable<string> messageTypes)
		{
		}
	}
}