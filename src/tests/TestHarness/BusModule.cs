namespace TestHarness
{
	using System;
	using System.Collections.Generic;
	using Autofac;
	using NanoMessageBus;
	using NanoMessageBus.Endpoints;
	using NanoMessageBus.Handlers;
	using NanoMessageBus.SubscriptionStorage;
	using NanoMessageBus.Transports;
	using NanoMessageBus.Transports.MessageQueue;

	public class BusModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder
				.Register(c =>
				{
					var container = c.Resolve<ILifetimeScope>().BeginLifetimeScope();
					return new MessageRouter(
						container,
						container.Resolve<IHandleUnitOfWork>(),
						container.Resolve<ITransportMessages>(),
						container.Resolve<ITrackMessageHandlers>(),
						container.Resolve<IHandlePoisonMessages>());
				})
				.As<IRouteMessagesToHandlers>()
				.As<IMessageContext>()
				.InstancePerLifetimeScope();

			builder
				.Register(c =>
				{
					c = c.Resolve<IComponentContext>();
					return new MessageQueueTransport(
						() => c.Resolve<IReceiveMessages>(),
						c.Resolve<ISendToEndpoints>(),
						Threads);
				})
				.As<ITransportMessages>()
				.SingleInstance();

			builder
				.Register(c =>
				{
					c = c.Resolve<IComponentContext>();
					return new MessageReceiverWorkerThread(
						c.Resolve<IReceiveFromEndpoints>(),
						() => c.Resolve<IRouteMessagesToHandlers>(),
						action => new BackgroundThread(action));
				})
				.As<IReceiveMessages>()
				.InstancePerDependency();

			builder
				.Register(c => new TransactionalBus(
					c.Resolve<IHandleUnitOfWork>(),
					c.Resolve<MessageBus>()))
				.As<IPublishMessages>()
				.As<ISendMessages>()
				.InstancePerLifetimeScope();

			var recipients = new Dictionary<Type, ICollection<Uri>>();
			var routes = recipients[typeof(string)] = new LinkedList<Uri>();
			routes.Add(new Uri("rabbitmq://localhost/MyExchange"));

			builder
				.Register(c => new MessageBus(
					c.Resolve<ITransportMessages>(),
					c.Resolve<IStoreSubscriptions>(),
					recipients,
					c.Resolve<IMessageContext>(),
					c.Resolve<MessageBuilder>(),
					new MessageTypeDiscoverer()))
				.As<MessageBus>()
				.InstancePerLifetimeScope();

			builder
				.Register(c => new MessageBuilder(c.Resolve<IAppendHeaders>(), LocalAddress))
				.As<MessageBuilder>()
				.InstancePerLifetimeScope();

			builder
				.Register(c => new NullHeaderAppender())
				.As<IAppendHeaders>()
				.SingleInstance();

			builder
				.Register(c =>
				{
					c = c.Resolve<IComponentContext>();
					return new MessageHandlerTable<IComponentContext>(c, new MessageTypeDiscoverer());
				})
				.As<ITrackMessageHandlers>()
				.SingleInstance();
		}

		private const int Threads = 3;
		private static readonly Uri LocalAddress = new Uri("rabbitmq://localhost/?MyQueue");
	}
}