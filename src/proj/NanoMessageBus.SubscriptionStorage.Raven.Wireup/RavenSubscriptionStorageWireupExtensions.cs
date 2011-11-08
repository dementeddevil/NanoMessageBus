namespace NanoMessageBus
{
	using Raven.Client;
	using Raven.Client.Document;
	using SubscriptionStorage.Raven;

	public static class RavenSubscriptionStorageWireupExtensions
	{
		public static SubscriptionStorageWireup WithRavenSubscriptionStorage(
			this SubscriptionStorageWireup wireup, string connectionStringName)
		{
			IDocumentStore store = new DocumentStore { ConnectionStringName = connectionStringName };
			store.Initialize();
			return wireup.WithRavenSubscriptionStorage(store);
		}

		public static SubscriptionStorageWireup WithRavenSubscriptionStorage(
			this SubscriptionStorageWireup wireup, IDocumentStore store)
		{
			return wireup.WithCustomSubscriptionStorage(new RavenSubscriptionStorage(store));
		}
	}
}