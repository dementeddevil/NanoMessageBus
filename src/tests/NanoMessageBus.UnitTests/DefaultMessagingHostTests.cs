#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject(typeof(DefaultMessagingHost))]
	public class when_a_null_set_of_connectors_is_provided_during_construction : with_the_messaging_host
	{
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => new DefaultMessagingHost(null, EmptyFactory));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_an_empty_set_of_connectors_is_provided_during_construction : with_the_messaging_host
	{
		static Exception thrown;

		Establish context = () =>
			mockConnectors.Clear();

		Because of = () =>
			thrown = Catch.Exception(() => 
				new DefaultMessagingHost(Connectors, EmptyFactory));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_no_channel_group_factory_is_provided_during_construction : with_the_messaging_host
	{
		static Exception thrown;

		Establish context = () =>
			mockConnectors.Clear();

		Because of = () =>
			thrown = Catch.Exception(() => new DefaultMessagingHost(Connectors, null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_initializing_the_host : with_the_messaging_host
	{
		static readonly Mock<IChannelConfiguration> config0 = new Mock<IChannelConfiguration>();
		static readonly Mock<IChannelConfiguration> config1 = new Mock<IChannelConfiguration>();
		static readonly Mock<IChannelConfiguration> config2 = new Mock<IChannelConfiguration>();

		Establish context = () =>
		{
			mockConnectors.Clear();

			mockConnectors.Add(new Mock<IChannelConnector>());
			mockConnectors[0].SetupGet(x => x.ChannelGroups).Returns(new[] { config0.Object, config1.Object });
			mockConnectors.Add(new Mock<IChannelConnector>());
			mockConnectors[1].SetupGet(x => x.ChannelGroups).Returns(new[] { config2.Object });

			config0.SetupGet(x => x.ChannelGroup).Returns("config0");
			config1.SetupGet(x => x.ChannelGroup).Returns("config1");
			config2.SetupGet(x => x.ChannelGroup).Returns("config2");

			mockFactory = new Mock<DefaultChannelGroupFactory>();
			mockFactory.Setup(x => x.Build(mockConnectors[0].Object, config0.Object));
			mockFactory.Setup(x => x.Build(mockConnectors[0].Object, config1.Object));
			mockFactory.Setup(x => x.Build(mockConnectors[1].Object, config2.Object));

			RebuildHost();
		};

		Because of = () =>
		    host.Initialize();

		It should_obtain_a_list_of_channel_groups_from_each_underlying_connector = () =>
			mockConnectors.ToList().ForEach(x => x.VerifyGet(mock => mock.ChannelGroups));
			
		It should_provide_each_config_and_its_associated_connector_to_the_factory = () =>
			mockFactory.VerifyAll();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_initializing_the_host_more_than_once : with_the_messaging_host
	{
		static readonly Mock<IChannelConfiguration> config0 = new Mock<IChannelConfiguration>();

		Establish context = () =>
		{
			mockConnectors.Add(new Mock<IChannelConnector>());
			mockConnectors[0].SetupGet(x => x.ChannelGroups).Returns(new[] { config0.Object });
			config0.SetupGet(x => x.ChannelGroup).Returns("config0");

			mockFactory = new Mock<DefaultChannelGroupFactory>();
			mockFactory.Setup(x => x.Build(mockConnectors[0].Object, config0.Object));

			RebuildHost();
		};

		private Because of = () =>
		{
			host.Initialize();
			host.Initialize();
			host.Initialize();
		};

		It should_do_nothing = () =>
			mockFactory.Verify(x => x.Build(Connectors[0], config0.Object), Times.Once());
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_initializing_a_disposed_host : with_the_messaging_host
	{
		static Exception thrown;

		Establish context = () =>
			host.Dispose();

		Because of = () =>
			thrown = Catch.Exception(() => host.Initialize());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_instructed_to_begin_receiving_messages : with_the_messaging_host
	{
		static readonly Action<IDeliveryContext> callback = channel => { };

		Establish context = () =>
		{
			mockGroup.Setup(x => x.BeginReceive(callback));
			host.Initialize();
		};

		Because of = () =>
			host.BeginReceive(callback);

		It should_pass_the_callback_to_the_underlying_connection_groups = () =>
			mockGroup.Verify(x => x.BeginReceive(callback), Times.Once());
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_more_than_one_callback_has_been_provided_for_receiving_messages_from_the_host : with_the_messaging_host
	{
		static readonly Action<IDeliveryContext> callback = channel => { };
		static Exception thrown;

		Establish context = () =>
		{
			mockGroup.Setup(x => x.BeginReceive(callback));
			host.Initialize();
			host.BeginReceive(callback);
		};

		Because of = () =>
			thrown = Catch.Exception(() => host.BeginReceive(callback));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_instructed_to_begin_receiving_messages_without_providing_a_callback : with_the_messaging_host
	{
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => host.BeginReceive(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_attempting_to_begin_receiving_messages_without_first_initializing_the_host : with_the_messaging_host
	{
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => host.BeginReceive(c => { }));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_attempting_to_receive_messages_against_a_disposed_host : with_the_messaging_host
	{
		static Exception thrown;

		Establish context = () =>
		{
			host.Initialize();
			host.Dispose();
		};

		Because of = () =>
			thrown = Catch.Exception(() => host.BeginReceive(c => { }));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_requesting_a_channel_group_without_initializing_the_host : with_the_messaging_host
	{
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => host.GetChannelGroup(defaultGroupName));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_no_channel_group_is_requested : with_the_messaging_host
	{
		static Exception thrown;

		Establish context = () =>
			host.Initialize();

		Because of = () =>
			thrown = Catch.Exception(() => host.GetChannelGroup(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_the_requested_channel_group_doesnt_exist : with_the_messaging_host
	{
		static Exception thrown;

		Establish context = () =>
			host.Initialize();

		Because of = () =>
			thrown = Catch.Exception(() => host.GetChannelGroup("Some channel group that doesn't exist."));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<KeyNotFoundException>();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_requesting_a_channel_group_from_a_disposed_host : with_the_messaging_host
	{
		static Exception thrown;

		Establish context = () =>
			host.Dispose();

		Because of = () =>
			thrown = Catch.Exception(() => host.GetChannelGroup(defaultGroupName));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_requesting_a_named_channel_group : with_the_messaging_host
	{
		static IChannelDispatch group;

		Establish context = () =>
		{
			var otherConfig = new Mock<IChannelConfiguration>();
			otherConfig.Setup(x => x.ChannelGroup).Returns("some other group");
			mockConfigs.Add(otherConfig);

			var otherGroup = new Mock<IChannelGroup>().Object;
			mockFactory.Setup(x => x.Build(Connectors[0], otherConfig.Object)).Returns(otherGroup);

			host.Initialize();
		};

		Because of = () =>
			group = host.GetChannelGroup(defaultGroupName);

		It should_return_the_correct_instance = () =>
			ReferenceEquals(group, mockGroup.Object).ShouldBeTrue();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_disposing_the_host : with_the_messaging_host
	{
		Establish context = () =>
		{
			mockGroup.Setup(x => x.Dispose());
			host.Initialize();
		};

		Because of = () =>
			host.Dispose();

		It should_dispose_each_underlying_channel_group = () =>
			mockGroup.Verify(x => x.Dispose(), Times.Once());
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_disposing_the_host_more_than_once : with_the_messaging_host
	{
		Establish context = () =>
		{
			mockGroup.Setup(x => x.Dispose());
			host.Initialize();
			host.Dispose();
		};

		Because of = () =>
			host.Dispose();

		It should_do_nothing = () =>
			mockGroup.Verify(x => x.Dispose(), Times.Once());
	}

	public abstract class with_the_messaging_host
	{
		protected static readonly ChannelGroupFactory EmptyFactory = (c, cg) => null;
		protected static IList<Mock<IChannelConnector>> mockConnectors;
		protected static Mock<DefaultChannelGroupFactory> mockFactory;
		protected static string defaultGroupName;
		static Mock<IChannelConfiguration> mockConfig;
		protected static IList<Mock<IChannelConfiguration>> mockConfigs;
		protected static Mock<IChannelGroup> mockGroup;
		static ChannelGroupFactory channelFactory;
		protected static DefaultMessagingHost host;

		protected static IList<IChannelConnector> Connectors
		{
			get { return mockConnectors == null ? null : mockConnectors.Select(x => x.Object).ToList(); }
		}

		Establish context = () =>
		{
			defaultGroupName = "Test Configuration Group";
			mockConfig = new Mock<IChannelConfiguration>();
			mockGroup = new Mock<IChannelGroup>();
			
			mockConfig.Setup(x => x.ChannelGroup).Returns(defaultGroupName);
			mockConfigs = new List<Mock<IChannelConfiguration>> { mockConfig };

			mockConnectors = new List<Mock<IChannelConnector>> { new Mock<IChannelConnector>() };
			mockConnectors[0].Setup(x => x.ChannelGroups).Returns(mockConfigs.Select(x => x.Object));

			mockFactory = new Mock<DefaultChannelGroupFactory>();
			mockFactory.Setup(x => x.Build(Connectors[0], mockConfig.Object)).Returns(mockGroup.Object);

			RebuildHost();
		};
		protected static void RebuildHost()
		{
			if (channelFactory == null && mockFactory != null)
				channelFactory = (c, cfg) => mockFactory.Object.Build(c, cfg);

			channelFactory = channelFactory ?? EmptyFactory;
			host = new DefaultMessagingHost(Connectors, channelFactory);
		}

		Cleanup after = () =>
		{
			defaultGroupName = null;

			mockConfig = null;
			mockGroup = null;
			mockFactory = null;
			mockConnectors = null;

			channelFactory = null;
			host = null;
		};
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169