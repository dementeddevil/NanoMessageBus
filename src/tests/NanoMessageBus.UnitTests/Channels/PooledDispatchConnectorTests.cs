#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.Channels
{
	using System;
	using System.Linq;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject(typeof(PooledDispatchConnector))]
	public class when_a_null_connector_is_specified_during_construction : using_the_pooled_dispatch_connector
	{
		Because of = () =>
			Try(() => new PooledDispatchConnector(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(PooledDispatchConnector))]
	public class when_disposing_the_pooled_dispatch_connector : using_the_pooled_dispatch_connector
	{
		Because of = () =>
			connector.Dispose();

		It should_dispose_the_underlying_connector = () =>
			mockConnector.Verify(x => x.Dispose(), Times.Once());
	}

	[Subject(typeof(PooledDispatchConnector))]
	public class when_fetching_the_current_connection_state : using_the_pooled_dispatch_connector
	{
		Establish context = () =>
			mockConnector.Setup(x => x.CurrentState).Returns(ConnectionState.Unauthorized);

		Because of = () =>
			connectionState = connector.CurrentState;

		It should_invoke_the_underlying_connector = () =>
			connectionState.ShouldEqual(ConnectionState.Unauthorized);

		static ConnectionState connectionState;
	}

	[Subject(typeof(PooledDispatchConnector))]
	public class when_fetching_the_current_set_of_channel_group_configurations : using_the_pooled_dispatch_connector
	{
		Establish context = () => mockConnector.Setup(x => x.ChannelGroups).Returns(new[]
		{
			new Mock<IChannelGroupConfiguration>().Object,
			new Mock<IChannelGroupConfiguration>().Object
		});

		Because of = () =>
			configs = connector.ChannelGroups.ToArray();

		It should_invoke_the_underlying_connector = () =>
			configs.SequenceEqual(mockConnector.Object.ChannelGroups).ShouldBeTrue();

		static IChannelGroupConfiguration[] configs;
	}

	[Subject(typeof(PooledDispatchConnector))]
	public class when_a_full_duplex_channel_is_established : using_the_pooled_dispatch_connector
	{
		It should_NOT_wrap_the_channel;
	}

	[Subject(typeof(PooledDispatchConnector))]
	public class when_an_asynchronous_channel_is_established : using_the_pooled_dispatch_connector
	{
		It should_NOT_wrap_the_channel;
	}

	[Subject(typeof(PooledDispatchConnector))]
	public class when_a_sync_dispatch_only_channel_is_established_with_the_connector : using_the_pooled_dispatch_connector
	{
		Because of = () =>
			connectedChannel = connector.Connect(ChannelGroupName);

		It should_invoke_the_underlying_connector = () =>
			mockConnector.Verify(x => x.Connect(ChannelGroupName));

		It should_wrap_over_the_channel = () =>
			connectedChannel.ShouldBeOfType<PooledDispatchChannel>();

		It should_provide_the_current_state_index = () =>
			((PooledDispatchChannel)connectedChannel).State.ShouldEqual(0); // TODO

		static IMessagingChannel connectedChannel;
	}

	public abstract class using_the_pooled_dispatch_connector
	{
		Establish context = () =>
		{
			mockConnector = new Mock<IChannelConnector>();
			mockChannel = new Mock<IMessagingChannel>();
			mockConnector.Setup(x => x.Connect(ChannelGroupName)).Returns(mockChannel.Object);
			thrown = null;

			connector = new PooledDispatchConnector(mockConnector.Object);
		};

		protected static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		protected const string ChannelGroupName = "Test Group";
		protected static PooledDispatchConnector connector;
		protected static Mock<IChannelConnector> mockConnector;
		protected static Mock<IMessagingChannel> mockChannel;
		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169