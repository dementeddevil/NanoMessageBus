namespace NanoMessageBus.SubscriptionStorage
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// An in-memory subscription storage. Useful for debugging purposes.
	/// </summary>
	public class InMemorySubscriptionStorage : IStoreSubscriptions
	{
		private readonly List<Subscription> subscriptions = new List<Subscription>();

		private class Subscription
		{
			public readonly Uri Address;
			public readonly string MessageType;

			public Subscription(Uri address, string messageType)
			{
				this.Address = address;
				this.MessageType = messageType;
			}
		}

		public void Subscribe(Uri address, IEnumerable<string> messageTypes, DateTime? expiration)
		{
			lock (this.subscriptions)
			{
				foreach (var type in messageTypes)
				{
					this.subscriptions.RemoveAll(s => s.Address == address && s.MessageType == type);
					this.subscriptions.Add(new Subscription(address, type));
				}
			}
		}
		public void Unsubscribe(Uri address, IEnumerable<string> messageTypes)
		{
			lock (this.subscriptions)
			{
				this.subscriptions.RemoveAll(s => s.Address == address && messageTypes.Any(m => m == s.MessageType));
			}
		}
		public ICollection<Uri> GetSubscribers(IEnumerable<string> messageTypes)
		{
			lock (this.subscriptions)
			{
				return this.subscriptions
					.Where(s => messageTypes.Any(m => s.MessageType == m))
					.Select(s => s.Address).Distinct().ToList();
			}
		}
	}
}