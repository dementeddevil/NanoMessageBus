#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.RabbitChannel
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Machine.Specifications;
	using Moq;
	using RabbitMQ.Client;
	using RabbitMQ.Client.Exceptions;
	using It = Machine.Specifications.It;

	[Subject(typeof(RabbitConnector))]
	public class when_no_connection_factory_is_provided_during_construction : using_a_connector
	{
		Because of = () =>
			thrown = Catch.Exception(() =>
				new RabbitConnector(null, new[] { new Mock<RabbitChannelGroupConfiguration>().Object }));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(RabbitConnector))]
	public class when_no_channel_group_configurations_are_provided_during_construction : using_a_connector
	{
		Because of = () =>
			thrown = Catch.Exception(() => new RabbitConnector(new ConnectionFactory(), null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject(typeof(RabbitConnector))]
	public class when_an_empty_set_of_channel_group_configurations_are_provided_during_construction : using_a_connector
	{
		Because of = () =>
			thrown = Catch.Exception(() =>
				new RabbitConnector(new ConnectionFactory(), new RabbitChannelGroupConfiguration[0]));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject(typeof(RabbitConnector))]
	public class when_a_connector_is_constructed : using_a_connector
	{
		Establish context = () =>
		{
			var mockGroup2 = new Mock<RabbitChannelGroupConfiguration>();
			mockGroup2.Setup(x => x.GroupName).Returns("group 2");
			mockConfigs.Add(mockGroup2);

			Initialize();
		};

		It should_contain_each_channel_group_config_provided = () =>
			mockConfigs.Select(x => x.Object).SequenceEqual(connector.ChannelGroups).ShouldBeTrue();

		It should_be_in_a_closed_status = () =>
			connector.CurrentState.ShouldEqual(ConnectionState.Closed);
	}

	[Subject(typeof(RabbitConnector))]
	public class when_no_group_name_is_provided_during_a_connect_attempt : using_a_connector
	{
		Because of = () =>
			thrown = Catch.Exception(() => connector.Connect(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(RabbitConnector))]
	public class when_an_empty_group_name_is_provided_during_a_connect_attempt : using_a_connector
	{
		Because of = () =>
			thrown = Catch.Exception(() => connector.Connect(string.Empty));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject(typeof(RabbitConnector))]
	public class when_an_unknown_group_name_is_provided_during_a_connect_attempt : using_a_connector
	{
		Because of = () =>
			thrown = Catch.Exception(() => connector.Connect("this group doesn't exist."));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<KeyNotFoundException>();
	}

	[Subject(typeof(RabbitConnector))]
	public class when_opening_the_first_channel : using_a_connector
	{
		Establish context = () =>
		{
			var mockGroup2 = new Mock<RabbitChannelGroupConfiguration>();
			mockGroup2.Setup(x => x.GroupName).Returns("group 2");
			mockGroup2.Setup(x => x.InitializeBroker(mockDefaultChannel.Object));
			mockConfigs.Add(mockGroup2);

			Initialize();
		};

		Because of = () =>
			connector.Connect(DefaultGroupName);

		It should_open_a_connection = () =>
			mockFactory.Verify(x => x.CreateConnection(connector.MaxRedirects), Times.Once());

		It should_show_an_open_connection_status = () =>
			connector.CurrentState.ShouldEqual(ConnectionState.Open);

		It should_initialize_all_channel_group_configurations_against_the_model = () =>
			mockConfigs.ToList().ForEach(cfg => cfg.Verify(x => x.InitializeBroker(mockDefaultChannel.Object)));
	}

	[Subject(typeof(RabbitConnector))]
	public class when_opening_additional_channels : using_a_connector
	{
		Establish context = () =>
			connector.Connect(DefaultGroupName);

		Because of = () =>
			connector.Connect(DefaultGroupName);

		It should_utilize_the_existing_connection = () =>
			mockFactory.Verify(x => x.CreateConnection(connector.MaxRedirects), Times.Once());

		It should_show_an_open_connection_status = () =>
			connector.CurrentState.ShouldEqual(ConnectionState.Open);
	}

	[Subject(typeof(RabbitConnector))]
	public class when_opening_each_messaging_channel : using_a_connector
	{
		Because of = () =>
			channel = connector.Connect(DefaultGroupName);

		It should_return_a_messaging_channel = () =>
			channel.ShouldNotBeNull();

		It should_maintain_an_open_connection_status = () =>
			connector.CurrentState.ShouldEqual(ConnectionState.Open);
	}

	[Subject(typeof(RabbitConnector))]
	public class when_establish_the_underlying_connection_throws_an_exception : using_a_connector
	{
		Establish context = () =>
			mockFactory.Setup(x => x.CreateConnection(connector.MaxRedirects)).Throws(new Exception());

		Because of = () =>
			thrown = Catch.Exception(() => connector.Connect(DefaultGroupName));

		It should_mark_the_connector_status_as_closed = () =>
			connector.CurrentState.ShouldEqual(ConnectionState.Closed);

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ChannelConnectionException>();
	}

	[Subject(typeof(RabbitConnector))]
	public class when_establish_the_underlying_connection_throws_an_authentication_related_exception : using_a_connector
	{
		Establish context = () =>
			mockFactory
				.Setup(x => x.CreateConnection(connector.MaxRedirects))
				.Throws(new PossibleAuthenticationFailureException(string.Empty, null));

		Because of = () =>
			thrown = Catch.Exception(() => connector.Connect(DefaultGroupName));

		It should_mark_the_connector_status_as_unauthenticated = () =>
			connector.CurrentState.ShouldEqual(ConnectionState.Unauthenticated);

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ChannelConnectionException>();
	}

	[Subject(typeof(RabbitConnector))]
	public class when_initializing_the_channel_group_configurations_throws_an_exception : using_a_connector
	{
		Establish context = () =>
			mockDefaultConfig.Setup(x => x.InitializeBroker(mockDefaultChannel.Object)).Throws(raised);

		Because of = () =>
			thrown = Catch.Exception(() => connector.Connect(DefaultGroupName));

		It should_dispose_the_created_channel = () =>
			mockDefaultChannel.Verify(x => x.Dispose(), Times.Once());

		It should_shutdown_the_connection = () =>
			mockConnection.Verify(x => x.Dispose());

		It should_mark_the_connection_status_as_closed = () =>
			connector.CurrentState.ShouldEqual(ConnectionState.Closed);

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ChannelConnectionException>();

		It should_wrap_the_original_exception = () =>
			ReferenceEquals(thrown.InnerException, raised).ShouldBeTrue();

		static readonly Exception raised = new Exception("some exception");
	}

	public abstract class using_a_connector
	{
		Establish context = () =>
		{
			mockFactory = new Mock<ConnectionFactory>();
			mockConnection = new Mock<IConnection>();
			mockFactory.Setup(x => x.CreateConnection(Moq.It.IsAny<int>())).Returns(mockConnection.Object);

			mockDefaultConfig = new Mock<RabbitChannelGroupConfiguration>();
			mockDefaultConfig.Setup(x => x.GroupName).Returns(DefaultGroupName);
			mockConfigs = new LinkedList<Mock<RabbitChannelGroupConfiguration>>(new[] { mockDefaultConfig });

			mockDefaultChannel = new Mock<IModel>();
			mockConnection.Setup(x => x.CreateModel()).Returns(mockDefaultChannel.Object);

			Initialize();
		};

		protected static void Initialize()
		{
			connector = new RabbitConnector(mockFactory.Object, mockConfigs.Select(x => x.Object));
		}

		protected const string DefaultGroupName = "default group";
		protected static Mock<ConnectionFactory> mockFactory;
		protected static Mock<IConnection> mockConnection;
		protected static Mock<RabbitChannelGroupConfiguration> mockDefaultConfig;
		protected static Mock<IModel> mockDefaultChannel;
		protected static ICollection<Mock<RabbitChannelGroupConfiguration>> mockConfigs;
		protected static RabbitConnector connector;

		protected static IMessagingChannel channel;
		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169