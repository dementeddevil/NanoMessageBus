#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.Channels
{
	using System;
	using System.Linq;
	using Machine.Specifications;
	using Moq;
	using RabbitMQ.Client;
	using It = Machine.Specifications.It;

	[Subject(typeof(RabbitWireup))]
	public class when_specifying_the_max_shutdown_timeout : using_the_wireup
	{
		Because of = () =>
			wireup.WithShutdownTimout(TimeSpan.Zero);

		It should_contain_the_shutdown_timeout_specified = () =>
			wireup.ShutdownTimeout.ShouldEqual(TimeSpan.Zero);
	}

	[Subject(typeof(RabbitWireup))]
	public class when_specifying_the_a_negative_shutdown_timeout : using_the_wireup
	{
		Because of = () =>
			Try(() => wireup.WithShutdownTimout(TimeSpan.FromSeconds(-1)));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject(typeof(RabbitWireup))]
	public class when_no_shutdown_timeout_is_specified : using_the_wireup
	{
		It should_contain_the_default_value = () =>
			wireup.ShutdownTimeout.ShouldEqual(TimeSpan.FromSeconds(3));
	}

	[Subject(typeof(RabbitWireup))]
	public class when_specifying_an_endpoint_address : using_the_wireup
	{
		Establish context = () =>
			factory = new Mock<FailoverRabbitConnectionFactory>();

	    Because of = () =>
	        wireup
				.WithConnectionFactory(factory.Object)
				.AddEndpoint(address)
				.AddChannelGroup(x => x.WithGroupName("my group")).Build();

		It should_set_the_address_provided_on_the_connection_factory = () =>
			factory.Verify(x => x.AddEndpoint(address), Times.Once());

		It should_randomize_the_order_of_the_endpoints = () =>
			factory.Verify(x => x.RandomizeEndpoints(), Times.Once());

	    static readonly Uri address = new Uri("amqp://user:pass@localhost/vhost/");
		static Mock<FailoverRabbitConnectionFactory> factory;
	}

	[Subject(typeof(RabbitWireup))]
	public class when_specifying_a_strictly_ordered_endpoint_address : using_the_wireup
	{
		Establish context = () =>
			factory = new Mock<FailoverRabbitConnectionFactory>();

		Because of = () =>
			wireup
				.WithConnectionFactory(factory.Object)
				.AddEndpoint(address, ordered: true)
				.AddChannelGroup(x => x.WithGroupName("my group")).Build();

		It should_set_the_address_provided_on_the_connection_factory = () =>
			factory.Verify(x => x.AddEndpoint(address), Times.Once());

		It should_NOT_randomize_the_order_of_the_endpoints = () =>
			factory.Verify(x => x.RandomizeEndpoints(), Times.Never());

		static readonly Uri address = new Uri("amqp://user:pass@localhost/vhost/");
		static Mock<FailoverRabbitConnectionFactory> factory;
	}

	[Subject(typeof(RabbitWireup))]
	public class when_authentication_information_is_already_specified : using_the_wireup
	{
		Establish context = () => factory = new FailoverRabbitConnectionFactory
		{
			UserName = "existing",
			Password = null
		};

		Because of = () => wireup
			.AddEndpoint(address)
			.WithConnectionFactory(factory)
			.AddChannelGroup(x => x.WithGroupName("group"))
			.Build();

		It should_prefer_the_username_aleady_provided_over_the_username_in_the_address = () =>
			factory.UserName.ShouldEqual("existing");

		It should_prefer_the_password_associated_with_the_existing_username_over_the_password_in_the_address = () =>
			factory.Password.ShouldBeNull();

		static readonly Uri address = new Uri("amqp://user:pass@localhost/");
		static FailoverRabbitConnectionFactory factory;
	}

	[Subject(typeof(RabbitWireup))]
	public class when_specifying_a_null_endpoint_address : using_the_wireup
	{
		Because of = () =>
			Try(() => wireup.AddEndpoint(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(RabbitWireup))]
	public class when_adding_a_channel_group_configuration : using_the_wireup
	{
		Because of = () =>
			wireup.AddChannelGroup(g => g.WithGroupName("my group name"));

		It should_add_the_group_to_the_collection = () =>
			wireup.ChannelGroups.Count.ShouldEqual(1);

		It should_provide_the_group_to_the_caller = () =>
			wireup.ChannelGroups.First().GroupName.ShouldEqual("my group name");
	}

	[Subject(typeof(RabbitWireup))]
	public class when_no_channel_group_config_callback_is_provided : using_the_wireup
	{
		Because of = () =>
			Try(() => wireup.AddChannelGroup(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(RabbitWireup))]
	public class when_no_connection_factory_is_specified : using_the_wireup
	{
		It should_use_a_default_instance = () =>
			wireup.ConnectionFactory.ShouldNotBeNull();

		It should_be_unique_per_wireup_instance = () =>
			wireup.ConnectionFactory.ShouldNotEqual(new RabbitWireup().ConnectionFactory);
	}

	[Subject(typeof(RabbitWireup))]
	public class when_a_null_connection_factory_is_specified : using_the_wireup
	{
		Because of = () =>
			Try(() => wireup.WithConnectionFactory(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(RabbitWireup))]
	public class when_a_connection_factory_is_specified : using_the_wireup
	{
		Because of = () =>
			wireup.WithConnectionFactory(factory);

		It should_contain_the_connection_factory_specified = () =>
			factory.ShouldEqual(wireup.ConnectionFactory);

		static readonly FailoverRabbitConnectionFactory factory = new FailoverRabbitConnectionFactory();
	}

	[Subject(typeof(RabbitWireup))]
	public class when_building_a_connector : using_the_wireup
	{
		Establish context = () =>
		{
			factory = new FailoverRabbitConnectionFactory();

			wireup
				.AddChannelGroup(x => x.WithGroupName("1"))
				.AddChannelGroup(x => x.WithGroupName("2"))
				.WithConnectionFactory(factory)
				.AddEndpoint(address);
		};

		Because of = () =>
			connector = wireup.Build();

		It should_provide_the_channel_groups_to_the_connector = () => wireup.ChannelGroups.ToList()
			.ForEach(x => connector.ChannelGroups.Any(cg => cg.GroupName == x.GroupName).ShouldBeTrue());

		static readonly Uri address = new Uri("amqp-0-9://a-different-host:5672");
		static RabbitConnector connector;
		static FailoverRabbitConnectionFactory factory;
	}

	[Subject(typeof(RabbitWireup))]
	public class when_building_a_connector_without_having_an_endpoint_specified : using_the_wireup
	{
		Establish context = () => wireup
			.AddChannelGroup(x => x.WithGroupName("1"))
			.WithConnectionFactory(factory);

		Because of = () =>
			wireup.Build();

		It should_leave_endpoint_on_the_connection_factory_as_the_default_value = () =>
			factory.Endpoint.ToString().ShouldEqual("amqp-0-9://localhost:5672");

		static readonly FailoverRabbitConnectionFactory factory = new FailoverRabbitConnectionFactory();
	}

	[Subject(typeof(RabbitWireup))]
	public class when_indicating_client_identity_should_be_asserted_with_a_certificate : using_the_wireup
	{
		Establish context = () => wireup
			.WithConnectionFactory(factory);

		Because of = () =>
			wireup.WithCertificateAuthentication();

		It should_attempt_external_rabbit_authentication_first = () =>
			factory.AuthMechanisms.First().ShouldBeOfType<ExternalMechanismFactory>();

		It should_attempt_plain_rabbit_authentication_first = () =>
			factory.AuthMechanisms.Last().ShouldBeOfType<PlainMechanismFactory>();

		static readonly FailoverRabbitConnectionFactory factory = new FailoverRabbitConnectionFactory();
	}

	public abstract class using_the_wireup
	{
		Establish context = () =>
		{
			wireup = new RabbitWireup();
		};

		protected static void Try(Action action)
		{
			thrown = Catch.Exception(action);
		}

		protected static RabbitWireup wireup;
		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169