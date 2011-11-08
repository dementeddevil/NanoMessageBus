namespace NanoMessageBus.MessageSubscribers
{
	using System;
	using SubscriptionStorage;

	public class SubscriptionMessageHandlers : IHandleMessages<SubscriptionRequestMessage>,
		IHandleMessages<UnsubscribeRequestMessage>
	{
		public virtual void Handle(SubscriptionRequestMessage message)
		{
			this.storage.Subscribe(this.subscriberAddress, message.MessageTypes, message.Expiration);
		}
		public virtual void Handle(UnsubscribeRequestMessage message)
		{
			this.storage.Unsubscribe(this.subscriberAddress, message.MessageTypes);
		}

		public SubscriptionMessageHandlers(IStoreSubscriptions storage, IMessageContext context)
		{
			this.storage = storage;
			this.subscriberAddress = context.CurrentMessage.ReturnAddress;
		}

		private readonly IStoreSubscriptions storage;
		private readonly Uri subscriberAddress;
	}
}