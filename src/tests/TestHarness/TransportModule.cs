namespace TestHarness
{
	using Autofac;
	using NanoMessageBus.Endpoints;
	using NanoMessageBus.Handlers;
	using NanoMessageBus.RabbitMQ;
	using NanoMessageBus.Serialization;
	using NanoMessageBus.SubscriptionStorage;

	public class TransportModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder
				.Register(c => new RabbitWireup()
					.ConnectAnonymouslyToLocalhost()
					.ListenTo(InputQueue.Queue)
					.MessagesPerChannel(32768)
					////.UseTransactions()
					.OpenConnection())
				.As<RabbitConnector>()
				.SingleInstance();

			builder
				.Register(c => c.Resolve<RabbitConnector>().Current)
				.As<RabbitChannel>()
				.InstancePerLifetimeScope() // TODO: how to open a channel with a send only endpoint?
				.ExternallyOwned(); // these need to live for as long as the thread does.

			builder
				.Register(c => c.Resolve<RabbitChannel>().UnitOfWork)
				.As<IHandleUnitOfWork>()
				.InstancePerLifetimeScope(); // TODO: how to do transactions with send only endpoints?

			builder
				.Register(c => new RabbitSenderEndpoint(
				    c.Resolve<RabbitConnector>(),
				    new BinarySerializer()))
				.As<ISendToEndpoints>()
				.SingleInstance();

			builder
				.Register(c => new RabbitReceiverEndpoint(
				    c.Resolve<RabbitConnector>(),
				    c.Resolve<RabbitFaultedMessageHandler>(),
				    contentType => new BinarySerializer())) // TODO
				.As<IReceiveFromEndpoints>()
				.As<IHandlePoisonMessages>()
				.SingleInstance();

			builder
				.Register(c => new RabbitFaultedMessageHandler(PoisonMessages, DeadLetters, MaxAttempts))
				.As<RabbitFaultedMessageHandler>()
				.SingleInstance();

			builder
				.Register(c => new RabbitSubscriptionStorage())
				.As<IStoreSubscriptions>()
				.SingleInstance();
		}

		private const int MaxAttempts = 3;
		private static readonly RabbitAddress InputQueue = new RabbitAddress("rabbitmq://localhost/?MyQueue");
		private static readonly RabbitAddress DeadLetters = new RabbitAddress("rabbitmq://localhost/dead-letter-exchange");
		private static readonly RabbitAddress PoisonMessages = new RabbitAddress("rabbitmq://localhost/poison-message-exchange");
	}
}