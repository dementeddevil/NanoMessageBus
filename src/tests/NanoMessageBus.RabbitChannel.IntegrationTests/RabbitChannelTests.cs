#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.RabbitChannel
{
	using System;
	using System.Configuration;
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

	public abstract class using_the_channel
	{
		Establish context = () =>
		{
			connectionUri = new Uri(ConfigurationManager.AppSettings["ConnectionUri"]);
			config = new RabbitChannelGroupConfiguration();
			connector = new RabbitConnector(factory, ShutdownTimeout, new[] { config });
		};

		protected static void Connect()
		{
			factory.Endpoint = new AmqpTcpEndpoint(connectionUri);
			channel = connector.Connect(config.GroupName);
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
		protected static RabbitConnector connector;
		protected static IMessagingChannel channel;
		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169