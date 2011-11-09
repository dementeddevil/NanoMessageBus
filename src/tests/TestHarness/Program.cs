namespace TestHarness
{
	using System;
	using System.Collections.Generic;
	using NanoMessageBus;
	using NanoMessageBus.Endpoints;
	using NanoMessageBus.RabbitMQ;
	using NanoMessageBus.Serialization;
	using RabbitMQ.Client;

	internal class Program
	{
		private static readonly IConnection Connection = new ConnectionFactory().CreateConnection();
		private static readonly ISerializer Serializer = new BinarySerializer();

		private static void Main()
		{
			Init();

			SendMessage();
			
			// ReceiveMessage();
			ReceiveAndSend();

			Connection.Dispose();
		}

		private static void Init()
		{
			using (var channel = Connection.CreateModel())
			{
				channel.ExchangeDeclare("MyExchange", ExchangeType.Fanout, true);
				channel.QueueDeclare("MyQueue", true, false, false, null);
				channel.QueueBind("MyQueue", "MyExchange", string.Empty);
				channel.QueuePurge("MyQueue");

				channel.ExchangeDeclare("dead-letter-exchange", ExchangeType.Fanout, true);
				channel.QueueDeclare("dlq", true, false, false, null);
				channel.QueueBind("dlq", "dead-letter-exchange", string.Empty);

				channel.ExchangeDeclare("poison-message-exchange", ExchangeType.Fanout, true);
				channel.QueueDeclare("pmq", true, false, false, null);
				channel.QueueBind("pmq", "poison-message-exchange", string.Empty);
			}
		}

		private static void SendMessage()
		{
			var address = new RabbitAddress("rabbitmq://localhost/MyExchange");

			using (var connector = new RabbitConnector(Connection, RabbitTransactionType.Full))
			using (var sender = new RabbitSenderEndpoint(() => connector, Serializer))
			using (var trx = connector.UnitOfWork)
			{
				var message = Build(address.Raw);
				sender.Send(message, address.Raw);
				trx.Complete();
			}
		}
		private static EnvelopeMessage Build(Uri returnAddress)
		{
			var messages = new List<object>(new object[] { SystemTime.UtcNow, "Hello, World!", 42 });

			return new EnvelopeMessage(
			    Guid.NewGuid(),
			    returnAddress,
				TimeSpan.MaxValue,
				true,
				null,
				messages);
		}

		private static void ReceiveMessage()
		{
			var address = new RabbitAddress("rabbitmq://localhost/?MyQueue");

			using (var connector = new RabbitConnector(Connection, RabbitTransactionType.Full, address))
			using (var receiver = OpenReceiver(connector))
			using (var trx = connector.UnitOfWork)
			{
				var message = receiver.Receive();
				trx.Complete();
			}
		}
		private static IReceiveFromEndpoints OpenReceiver(RabbitConnector connector)
		{
			var poisonMessageExchange = new RabbitAddress("rabbitmq://localhost/poison-message-exchange");
			var deadLetterExchange = new RabbitAddress("rabbitmq://localhost/dead-letter-exchange");

			return new RabbitReceiverEndpoint(
				() => connector,
				deadLetterExchange,
				poisonMessageExchange,
				x => Serializer,
				3);
		}

		private static void ReceiveAndSend()
		{
			var address = new RabbitAddress("rabbitmq://localhost/?MyQueue");

			using (var connector = new RabbitConnector(Connection, RabbitTransactionType.Full, address))
			using (var receiver = OpenReceiver(connector))
			using (var sender = new RabbitSenderEndpoint(() => connector, Serializer))
			using (var trx = connector.UnitOfWork)
			{
				var message = receiver.Receive();

				sender.Send(Build(address.Raw), new Uri("rabbitmq://localhost/MyExchange"));
				sender.Send(Build(address.Raw), new Uri("rabbitmq://localhost/dead-letter-exchange"));

				trx.Complete();
			}
		}
	}
}