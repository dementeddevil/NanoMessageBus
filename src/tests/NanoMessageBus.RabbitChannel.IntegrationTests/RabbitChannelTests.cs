#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.RabbitChannel
{
	using System;
	using System.Configuration;
	using Machine.Specifications;
	using RabbitMQ.Client;

	[Subject(typeof(RabbitChannel))]
	public class when_dispatching_a_message : using_the_channel
	{
		Establish context = () =>
		{
			config.WithMessageTypes(new[] { typeof(string) });
			Connect();
		};

		Because of = () =>
			thrown = Catch.Exception(() => channel.Send(null));

		It should_not_throw_an_exception = () =>
			thrown.ShouldBeNull();
	}

	public abstract class using_the_channel
	{
		Establish context = () =>
		{
			config = new RabbitChannelGroupConfiguration();
			connector = new RabbitConnector(factory, ShutdownTimeout, new[] { config });
		};

		protected static void Connect()
		{
			channel = connector.Connect(config.GroupName);
		}

		Cleanup after = () =>
		{
			channel.Dispose();
			connector.Dispose();
		};

		static readonly TimeSpan ShutdownTimeout = TimeSpan.FromMilliseconds(100);
		static readonly Uri ConnectionUri = new Uri(ConfigurationManager.AppSettings["ConnectionUri"]);
		static readonly ConnectionFactory factory = new ConnectionFactory();
		protected static RabbitChannelGroupConfiguration config;
		protected static RabbitConnector connector;
		protected static IMessagingChannel channel;
		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169