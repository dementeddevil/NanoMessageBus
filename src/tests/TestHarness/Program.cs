namespace TestHarness
{
	using System;
	using Autofac;
	using NanoMessageBus;
	using NanoMessageBus.Handlers;
	using NanoMessageBus.Transports;

	internal class Program : IHandleMessages<string>
	{
		private static void Main()
		{
			MessageHandlerTable<IComponentContext>.RegisterHandler(c => new Program());

			var builder = new ContainerBuilder();
			builder.RegisterModule(new BusModule());
			builder.RegisterModule(new TransportModule());

			using (var container = builder.Build())
			{
				var receiver = container.Resolve<IReceiveMessages>();
				receiver.Start();

				Console.WriteLine("Press any key to exit...");
				Console.ReadLine();
			}
		}

		public void Handle(string message)
		{
			Console.WriteLine(message);
		}
	}
}