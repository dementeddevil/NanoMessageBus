#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.RabbitChannel
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Linq;
	using System.Threading;
	using Machine.Specifications;
	using RabbitMQ.Client;

	[Subject(typeof(RabbitChannel))]
	public class when_connecting_to_a_nonexistent_instance : using_the_channel
	{
		Establish context = () =>
			connectionUri = new Uri(ConfigurationManager.AppSettings["NonexistentUri"]);

		Because of = () =>
			thrown = Catch.Exception(Connect);

		It should_throw_a_ChannelConnectionException = () =>
			thrown.ShouldBeOfType<ChannelConnectionException>();
	}

	[Ignore("TODO")]
	[Subject(typeof(RabbitChannel))]
	public class when_connecting_with_bad_credentials : using_the_channel
	{
		Establish context = () =>
			connectionUri = new Uri(ConfigurationManager.AppSettings["AuthenticationFailureUri"]);

		Because of = () =>
			thrown = Catch.Exception(Connect);

		It should_throw_a_ChannelConnectionException = () =>
			thrown.ShouldBeOfType<ChannelConnectionException>();
	}

	[Subject(typeof(RabbitChannel))]
	public class when_dispatching_a_message : using_the_channel
	{
		Establish context = () =>
		{
			var logicalMessages = new[] { "some message" };
			var recipients = new[] { new Uri("fanout://system.string/") };
			var message = new ChannelMessage(Guid.NewGuid(), Guid.NewGuid(), null, null, logicalMessages);
			envelope = new ChannelEnvelope(message, recipients);

			config.WithMessageTypes(new[] { typeof(string) });
			Connect();
		};

		Because of = () =>
			thrown = Catch.Exception(() => channel.Send(envelope));

		It should_not_throw_an_exception = () =>
			thrown.ShouldBeNull();

		static ChannelEnvelope envelope;
	}

	[Subject(typeof(RabbitChannel))]
	public class when_sending_and_then_receiving_a_message : using_the_channel
	{
		Establish context = () =>
		{
			configs.Add(new RabbitChannelGroupConfiguration()
				.WithGroupName("receive")
				.WithInputQueue("my-queue")
				.WithMessageTypes(new[] { typeof(Guid) })
				.WithCleanQueue());

			Connect(); // for send

			new Thread(() =>
			{
				var receiver = connector.Connect("receive");
				receiver.Receive(delivery =>
				{
					// sending to the PME throws an exception
					received = (Guid)delivery.CurrentMessage.Messages.First();
					receiver.BeginShutdown();
				});
			}).Start();
		};

		Because of = () =>
			channel.Send(BuildEnvelope(message, new Uri("fanout://system.guid")));

		It should_wait_a_little_bit_to_receive_the_message = () =>
			Thread.Sleep(50);

		It should_receive_the_message_that_was_sent = () =>
			received.ShouldEqual(message);

		static Guid received;
		static readonly Guid message = Guid.NewGuid();
	}

	public abstract class using_the_channel
	{
		Establish context = () =>
		{
			connector = null;
			connectionUri = new Uri(ConfigurationManager.AppSettings["ConnectionUri"]);
			config = new RabbitChannelGroupConfiguration();
			configs = new LinkedList<RabbitChannelGroupConfiguration>(new[] { config });
		};

		protected static void Connect()
		{
			connector = connector ?? new RabbitConnector(factory, ShutdownTimeout, configs);
			factory.Endpoint = new AmqpTcpEndpoint(connectionUri);
			channel = connector.Connect(config.GroupName);
		}
		protected static ChannelEnvelope BuildEnvelope(object message, params Uri[] recipients)
		{
			var channelMessage = new ChannelMessage(
				Guid.NewGuid(), Guid.NewGuid(), null, null, new[] { message });
			return new ChannelEnvelope(channelMessage, recipients);
		}

		Cleanup after = () =>
		{
			channel.Dispose();
			connector.Dispose();
		};

		static readonly TimeSpan ShutdownTimeout = TimeSpan.FromMilliseconds(100);
		static readonly ConnectionFactory factory = new ConnectionFactory();
		protected static Uri connectionUri;
		protected static RabbitChannelGroupConfiguration config;
		protected static ICollection<RabbitChannelGroupConfiguration> configs;
		protected static RabbitConnector connector;
		protected static IMessagingChannel channel;
		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169