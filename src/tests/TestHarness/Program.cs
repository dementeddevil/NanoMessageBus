namespace TestHarness
{
	using System;
	using System.Text;
	using System.Threading;
	using Autofac;
	using NanoMessageBus;
	using NanoMessageBus.Handlers;
	using NanoMessageBus.RabbitMQ;
	using NanoMessageBus.Transports;

	internal class Program : IHandleMessages<string>
	{
		private static void Main()
		{
			var builder = new ContainerBuilder();
			builder.RegisterModule(new BusModule());
			builder.RegisterModule(new TransportModule());

			using (var container = builder.Build())
			{
				Console.WriteLine("Press any key to send messages.");
				Console.ReadLine();
				Send(container);

				Console.WriteLine("Press any key to receive messages.");
				Console.ReadLine();
				Receive(container);

				Console.WriteLine("Press any key to exit.");
				Console.ReadLine();
			}
		}

		private static void Send(IContainer container)
		{
			using (var uow = container.Resolve<IHandleUnitOfWork>())
			{
				Send(container as IComponentContext);
				uow.Complete();
			}
		}
		private static void Send(IComponentContext container)
		{
			var sender = container.Resolve<ISendMessages>();
			for (var i = 0; i < 10000; i++)
				sender.Send("Hello, World!");
		}

		private static void Receive(IComponentContext container)
		{
			MessageHandlerTable<IComponentContext>.RegisterHandler(c => new Program());
			var receiver = container.Resolve<ITransportMessages>();
			receiver.StartListening();
		}

		private static readonly object locker = new object();
		private static int counter;

		public void Handle(string message)
		{
			lock (locker)
			{
				if (++counter % 100 == 0)
					Console.WriteLine(counter);
			}
		}
	}
}