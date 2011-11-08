namespace NanoMessageBus.SubscriptionStorage.Raven
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Transactions;
	using global::Raven.Client;

	public class RavenSubscriptionStorage : IStoreSubscriptions
	{
		private readonly IDocumentStore store;

		public RavenSubscriptionStorage(IDocumentStore store)
		{
			if (store == null)
				throw new ArgumentNullException("store");

			this.store = store;
		}

		public void Subscribe(Uri address, IEnumerable<string> messageTypes, DateTime? expiration)
		{
			if (address == null || messageTypes == null)
				return;

			using (var tx = NewTransaction())
			using (var session = this.store.OpenSession())
			{
				foreach (var messageType in messageTypes)
				{
					var type = messageType;

					var subscription = session.Query<Subscription>()
						.Where(s => s.Subscriber == address.ToString() && s.MessageType == type)
						.SingleOrDefault();

					if (subscription == null)
					{
						subscription = new Subscription(address.ToString(), messageType, expiration);
						session.Store(subscription);
					}
					else
						subscription.Expiration = expiration;
				}
				session.SaveChanges();
				tx.Complete();
			}
		}

		public void Unsubscribe(Uri address, IEnumerable<string> messageTypes)
		{
			if (address == null || messageTypes == null)
				return;

			using (var tx = NewTransaction())
			using (var session = this.store.OpenSession())
			{
				Remove(session, address.ToString(), messageTypes);
				session.SaveChanges();
				tx.Complete();
			}
		}

		public ICollection<Uri> GetSubscribers(IEnumerable<string> messageTypes)
		{
			using (SuppressTransaction())
			using (var session = this.store.OpenSession())
			{
				return messageTypes.SelectMany(mt => GetSubscribers(session, mt)).Distinct().ToList()
					.Select(s => new Uri(s)).ToList();
			}
		}

		private static void Remove(IDocumentSession session, string subscriber, IEnumerable<string> messageTypes)
		{
			foreach (var messageType in messageTypes)
				Remove(session, subscriber, messageType);
		}

		private static void Remove(IDocumentSession session, string subscriber, string messageType)
		{
			var subscription = session.Query<Subscription>()
				.Where(s => s.Subscriber == subscriber && s.MessageType == messageType).SingleOrDefault();
			if (subscription != null)
				session.Delete(subscription);
		}

		private static IEnumerable<string> GetSubscribers(IDocumentSession session, string messageType)
		{
			return session.Query<Subscription>().Customize(c => c.WaitForNonStaleResults())
				.Where(s => s.MessageType == messageType)
				.ToArray()
				.Select(s => s.Subscriber);
		}

		private static TransactionScope NewTransaction()
		{
			var options = new TransactionOptions
			{
				IsolationLevel = IsolationLevel.ReadCommitted
			};
			return new TransactionScope(TransactionScopeOption.RequiresNew, options);
		}

		private static IDisposable SuppressTransaction()
		{
			var options = new TransactionOptions
			{
				IsolationLevel = IsolationLevel.ReadCommitted
			};
			return new TransactionScope(TransactionScopeOption.Suppress, options);
		}
	}
}