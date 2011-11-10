namespace TestHarness
{
	using Autofac;
	using NanoMessageBus.Endpoints;
	using NanoMessageBus.Handlers;
	using NanoMessageBus.RabbitMQ;
	using NanoMessageBus.Serialization;
	using NanoMessageBus.SubscriptionStorage;
	using RabbitMQ.Client;

	public class TransportModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder
				.Register(c => new BinarySerializer())
				.As<ISerializer>()
				.SingleInstance();

			builder
				.Register(c => new ConnectionFactory().CreateConnection())
				.As<IConnection>()
				.SingleInstance();

			builder
				.Register(c =>
				{
					var container = c.Resolve<ILifetimeScope>();
					return new RabbitConnector(c.Resolve<IConnection>(), TransactionType, InputQueue);
				})
				.As<RabbitConnector>()
				.InstancePerLifetimeScope();

			builder
				.Register(c => c.Resolve<RabbitConnector>().UnitOfWork)
				.As<IHandleUnitOfWork>()
				.InstancePerLifetimeScope();

			builder
				.Register(c =>
				{
					c = c.Resolve<IComponentContext>();
					return new RabbitSenderEndpoint(
						() => c.Resolve<RabbitConnector>(),
						c.Resolve<ISerializer>());
				})
				.As<ISendToEndpoints>()
				.SingleInstance();

			builder
				.Register(c =>
				{
					c = c.Resolve<IComponentContext>();
					return new RabbitReceiverEndpoint(
						() => c.Resolve<RabbitConnector>(),
						() => c.Resolve<RabbitFaultedMessageHandler>(),
						contentType => c.Resolve<ISerializer>());
				})
				.As<IReceiveFromEndpoints>()
				.As<IHandlePoisonMessages>()
				.SingleInstance();

			builder
				.Register(c => new RabbitFaultedMessageHandler(
					c.Resolve<RabbitConnector>(), DeadLetters, PoisonMessages, MaxAttempts))
				.As<RabbitFaultedMessageHandler>()
				.InstancePerLifetimeScope();

			builder
				.Register(c => new RabbitSubscriptionStorage())
				.As<IStoreSubscriptions>()
				.SingleInstance();
		}

		private const int MaxAttempts = 3;
		private static readonly RabbitAddress InputQueue = new RabbitAddress("rabbitmq://localhost/?MyQueue");
		private static readonly RabbitAddress DeadLetters = new RabbitAddress("rabbitmq://localhost/dead-letter-exchange");
		private static readonly RabbitAddress PoisonMessages = new RabbitAddress("rabbitmq://localhost/poison-message-exchange");
		private const RabbitTransactionType TransactionType = RabbitTransactionType.Full;
	}
}