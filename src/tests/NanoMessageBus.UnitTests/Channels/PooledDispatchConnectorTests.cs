#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections.Generic;
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
		Because of = () =>
			configs = connector.ChannelGroups.ToArray();

		It should_return_the_set_of_configurations_from_the_underlying_connector = () =>
			configs.SequenceEqual(mockConnector.Object.ChannelGroups).ShouldBeTrue();

		static IChannelGroupConfiguration[] configs;
	}

	[Subject(typeof(PooledDispatchConnector))]
	public class when_disposing_the_pooled_connector : using_the_pooled_dispatch_connector
	{
		Because of = () =>
			connector.Dispose();

		It should_dispose_the_underlying_connector = () =>
			mockConnector.Verify(x => x.Dispose(), Times.Once());
	}

	[Subject(typeof(PooledDispatchConnector))]
	public class when_disposing_the_pooled_connector_multiple_times : using_the_pooled_dispatch_connector
	{
		Establish context = () =>
			connector.Dispose();

		Because of = () =>
			connector.Dispose();

		It should_dispose_the_underlying_connector_once = () =>
			mockConnector.Verify(x => x.Dispose(), Times.Once());
	}

	[Subject(typeof(PooledDispatchConnector))]
	public class when_attempting_to_open_a_pooled_channel_against_a_disposed_connector : using_the_pooled_dispatch_connector
	{
		Establish context = () =>
			connector.Dispose();

		Because of = () =>
			Try(() => connector.Connect(PooledGroupName));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(PooledDispatchConnector))]
	public class when_attempting_to_open_a_non_pooled_channel_against_a_disposed_connector : using_the_pooled_dispatch_connector
	{
		Establish context = () =>
			connector.Dispose();

		Because of = () =>
			Try(() => connector.Connect(IgnoredGroupName));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(PooledDispatchConnector))]
	public class when_a_non_pooled_channel_is_established : using_the_pooled_dispatch_connector
	{
		Because of = () =>
			connectedChannel = connector.Connect(IgnoredGroupName);

		It should_invoke_the_underlying_connector = () =>
			mockConnector.Verify(x => x.Connect(IgnoredGroupName), Times.Once());

		It should_NOT_wrap_the_channel = () =>
			connectedChannel.ShouldNotBeOfType(typeof(PooledDispatchChannel));

		static IMessagingChannel connectedChannel;
	}

	[Subject(typeof(PooledDispatchConnector))]
	public class when_no_pooled_channels_are_available : using_the_pooled_dispatch_connector
	{
		Because of = () =>
			connectedChannel = connector.Connect(PooledGroupName);

		It should_invoke_the_underlying_connector = () =>
			mockConnector.Verify(x => x.Connect(PooledGroupName), Times.Once());

		It should_wrap_the_channel = () =>
			connectedChannel.ShouldBeOfType<PooledDispatchChannel>();

		static IMessagingChannel connectedChannel;
	}

	[Subject(typeof(PooledDispatchConnector))]
	public class when_attempting_to_release_a_null_channel : using_the_pooled_dispatch_connector
	{
		Because of = () =>
			Try(() => connector.Release(null, 0));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(PooledDispatchConnector))]
	public class when_attempting_to_release_channel_not_originating_from_this_connector : using_the_pooled_dispatch_connector
	{
		Because of = () =>
			Try(() => connector.Release(Create(Config(PooledGroupName, true, true).Object).Object, 0));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(PooledDispatchConnector))]
	public class when_a_pooled_channel_is_disposed_and_the_underlying_channel_made_available : using_the_pooled_dispatch_connector
	{
		Establish context = () =>
			connector.Connect(PooledGroupName).Dispose();

		Because of = () =>
			connectedChannel = connector.Connect(PooledGroupName);

		It should_only_invoke_the_underlying_connector_for_the_first_channel = () =>
			mockConnector.Verify(x => x.Connect(PooledGroupName), Times.Once());

		It should_wrap_the_channel = () =>
			connectedChannel.ShouldBeOfType<PooledDispatchChannel>();

		static IMessagingChannel connectedChannel;
	}

	[Subject(typeof(PooledDispatchConnector))]
	public class when_attempting_to_teardown_a_null_channel : using_the_pooled_dispatch_connector
	{
		Because of = () =>
			Try(() => connector.Teardown(null, 0));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(PooledDispatchConnector))]
	public class when_attempting_to_teardown_channel_not_originating_from_this_connector : using_the_pooled_dispatch_connector
	{
		Because of = () =>
			Try(() => connector.Teardown(Create(Config(PooledGroupName, true, true).Object).Object, 0));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(PooledDispatchConnector))]
	public class when_tearing_down_a_channel : using_the_pooled_dispatch_connector
	{
		Establish context = () =>
		{
			var channels = new List<IMessagingChannel>
			{
				connector.Connect(PooledGroupName), // returned to the pool
				connector.Connect(PooledGroupName), // returned to the pool
				connector.Connect(PooledGroupName), // still outstanding, shouldn't be disposed
				connector.Connect(PooledGroupName), // tearing down
			};

			channels.Skip(0).First().Dispose();
			channels.Skip(1).First().Dispose();
		};

		Because of = () => 
			connector.Teardown(mockChannels.Last().Object, 0);

		It should_dispose_the_underlying_channel_being_torn_down = () =>
			mockChannels.Last().Verify(x => x.Dispose(), Times.Once());

		It should_dispose_the_underlying_channel_of_any_uncommitted_available_channels = () =>
			mockChannels.Take(2).ToList().ForEach(mock => mock.Verify(x => x.Dispose(), Times.Once()));

		It should_NOT_dispose_the_underlying_channel_of_any_outstanding_channels = () =>
			mockChannels.Skip(2).First().Verify(x => x.Dispose(), Times.Never());
	}

	[Subject(typeof(PooledDispatchConnector))]
	public class when_opening_a_channel_after_a_teardown_has_occurred : using_the_pooled_dispatch_connector
	{
		Establish context = () =>
		{
			connector.Connect(PooledGroupName);
			connector.Teardown(mockChannels[0].Object, 0);
		};

		Because of = () =>
			connector.Connect(PooledGroupName);

		It should_invoke_the_underlying_connector_for_a_new_channel = () =>
			mockConnector.Verify(x => x.Connect(PooledGroupName), Times.Exactly(2));
	}

	public abstract class using_the_pooled_dispatch_connector
	{
		Establish context = () =>
		{
			var pooledConfig = Config(PooledGroupName, true, true);
			var ignoredConfig = Config(IgnoredGroupName, false, false);
			mockConfigs = new List<Mock<IChannelGroupConfiguration>> { pooledConfig, ignoredConfig };

			mockConnector = new Mock<IChannelConnector>();
			mockConnector.Setup(x => x.ChannelGroups).Returns(mockConfigs.Select(x => x.Object));

			mockChannels = new List<Mock<IMessagingChannel>>();
			mockConnector
				.Setup(x => x.Connect(Moq.It.IsAny<string>()))
				.Returns(() => Create(Config(PooledGroupName, true, true).Object).Object);

			thrown = null;

			connector = new PooledDispatchConnector(mockConnector.Object);
		};
		protected static Mock<IChannelGroupConfiguration> Config(string name, bool dispatchOnly, bool synchronous)
		{
			var mock = new Mock<IChannelGroupConfiguration>();
			mock.Setup(x => x.GroupName).Returns(name);
			mock.Setup(x => x.DispatchOnly).Returns(dispatchOnly);
			mock.Setup(x => x.Synchronous).Returns(synchronous);
			return mock;
		}
		protected static Mock<IMessagingChannel> Create(IChannelGroupConfiguration config)
		{
			var mock = new Mock<IMessagingChannel>();
			mock.Setup(x => x.CurrentConfiguration).Returns(config);
			mockChannels.Add(mock);
			return mock;
		}

		protected static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		protected const string PooledGroupName = "Pooled Group";
		protected const string IgnoredGroupName = "Non-proxied Group";
		protected static PooledDispatchConnector connector;
		protected static Mock<IChannelConnector> mockConnector;
		protected static List<Mock<IMessagingChannel>> mockChannels;
		protected static List<Mock<IChannelGroupConfiguration>> mockConfigs;
		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169