#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Linq;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject(typeof(DefaultMessagingHost))]
	public class when_a_null_set_of_connectors_is_provided_during_construction : with_the_messaging_host
	{
		Because of = () =>
			thrown = Catch.Exception(() => new DefaultMessagingHost(null, EmptyFactory));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_an_empty_set_of_connectors_is_provided_during_construction : with_the_messaging_host
	{
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
		Establish context = () =>
		{
			mockConnectors.Clear();

			mockConnectors.Add(new Mock<IChannelConnector>());
			mockConnectors[0].SetupGet(x => x.ChannelGroups).Returns(new[] { config0.Object, config1.Object });
			mockConnectors.Add(new Mock<IChannelConnector>());
			mockConnectors[1].SetupGet(x => x.ChannelGroups).Returns(new[] { config2.Object });

			config0.SetupGet(x => x.GroupName).Returns("config0");
			config1.SetupGet(x => x.GroupName).Returns("config1");
			config2.SetupGet(x => x.GroupName).Returns("config2");

			mockFactory = new Mock<DefaultChannelGroupFactory>();
			mockFactory.Setup(x => x.Build(mockConnectors[0].Object, config0.Object)).Returns(mockGroup.Object);
			mockFactory.Setup(x => x.Build(mockConnectors[0].Object, config1.Object)).Returns(mockGroup.Object);
			mockFactory.Setup(x => x.Build(mockConnectors[1].Object, config2.Object)).Returns(mockGroup.Object);

			RebuildHost();
		};

		Because of = () =>
			host.Initialize();

		It should_obtain_a_list_of_channel_groups_from_each_underlying_connector = () =>
			mockConnectors.ToList().ForEach(x => x.VerifyGet(mock => mock.ChannelGroups));
			
		It should_provide_each_config_and_its_associated_connector_to_the_factory = () =>
			mockFactory.VerifyAll();

		It should_initialize_each_channel_group = () =>
			mockGroup.Verify(x => x.Initialize(), Times.Exactly(3)); // tests provide the same group 3 times

		static readonly Mock<IChannelGroupConfiguration> config0 = new Mock<IChannelGroupConfiguration>();
		static readonly Mock<IChannelGroupConfiguration> config1 = new Mock<IChannelGroupConfiguration>();
		static readonly Mock<IChannelGroupConfiguration> config2 = new Mock<IChannelGroupConfiguration>();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_initializing_the_host_more_than_once : with_the_messaging_host
	{
		Establish context = () =>
		{
			mockConnectors.Add(new Mock<IChannelConnector>());
			mockConnectors[0].SetupGet(x => x.ChannelGroups).Returns(new[] { config0.Object });
			config0.SetupGet(x => x.GroupName).Returns("config0");

			mockFactory = new Mock<DefaultChannelGroupFactory>();
			mockFactory.Setup(x => x.Build(mockConnectors[0].Object, config0.Object)).Returns(mockGroup.Object);

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

		static readonly Mock<IChannelGroupConfiguration> config0 = new Mock<IChannelGroupConfiguration>();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_initializing_a_disposed_host : with_the_messaging_host
	{
		Establish context = () =>
			host.Dispose();

		Because of = () =>
			thrown = Catch.Exception(() => host.Initialize());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();
	}

	public class when_initializing_does_not_create_any_channel_groups : with_the_messaging_host
	{
		Establish context = () =>
			mockConfigs.Clear();

		Because of = () =>
			thrown = Catch.Exception(() => host.Initialize());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ConfigurationErrorsException>();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_instructed_to_begin_receiving_messages : with_the_messaging_host
	{
		Establish context = () =>
		{
			var otherConfig = new Mock<IChannelGroupConfiguration>();
			otherConfig.Setup(x => x.GroupName).Returns("dispatch-only group");
			mockConfigs.Add(otherConfig);

			otherGroup.Setup(x => x.DispatchOnly).Returns(true); // default group is dispatch-only
			mockFactory.Setup(x => x.Build(Connectors[0], otherConfig.Object)).Returns(otherGroup.Object);

			mockGroup.Setup(x => x.BeginReceive(callback));

			host.Initialize();
		};

		Because of = () =>
			host.BeginReceive(callback);

		It should_pass_the_callback_to_the_full_duplex_channel_groups = () =>
			mockGroup.Verify(x => x.BeginReceive(callback), Times.Once());

		It should_NOT_pass_the_callback_to_the_dispatch_only_channel_groups = () =>
			otherGroup.Verify(x => x.BeginReceive(callback), Times.Never());

		static readonly Mock<IChannelGroup> otherGroup = new Mock<IChannelGroup>();
		static readonly Action<IDeliveryContext> callback = channel => { };
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_more_than_one_callback_has_been_provided_for_receiving_messages_from_the_host : with_the_messaging_host
	{
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

		static readonly Action<IDeliveryContext> callback = channel => { };
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_instructed_to_begin_receiving_messages_without_providing_a_callback : with_the_messaging_host
	{
		Because of = () =>
			thrown = Catch.Exception(() => host.BeginReceive(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_attempting_to_begin_receiving_messages_without_first_initializing_the_host : with_the_messaging_host
	{
		Because of = () =>
			thrown = Catch.Exception(() => host.BeginReceive(c => { }));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_attempting_to_receive_messages_against_a_disposed_host : with_the_messaging_host
	{
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
		Because of = () =>
			thrown = Catch.Exception(() => host.GetOutboundChannel(defaultGroupName));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_no_channel_group_is_requested : with_the_messaging_host
	{
		Establish context = () =>
			host.Initialize();

		Because of = () =>
			thrown = Catch.Exception(() => host.GetOutboundChannel(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_the_requested_channel_group_doesnt_exist : with_the_messaging_host
	{
		Establish context = () =>
			host.Initialize();

		Because of = () =>
			thrown = Catch.Exception(() => host.GetOutboundChannel("Some channel group that doesn't exist."));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<KeyNotFoundException>();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_requesting_a_channel_group_from_a_disposed_host : with_the_messaging_host
	{
		Establish context = () =>
			host.Dispose();

		Because of = () =>
			thrown = Catch.Exception(() => host.GetOutboundChannel(defaultGroupName));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_requesting_a_full_duplex_channel_group_for_dispatch : with_the_messaging_host
	{
		Establish context = () =>
			host.Initialize();

		Because of = () =>
			thrown = Catch.Exception(() => host.GetOutboundChannel(defaultGroupName));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<KeyNotFoundException>();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_requesting_a_named_channel_group : with_the_messaging_host
	{
		Establish context = () =>
		{
			var otherConfig = new Mock<IChannelGroupConfiguration>();
			otherConfig.Setup(x => x.GroupName).Returns("some other group");
			mockConfigs.Add(otherConfig);

			mockGroup.Setup(x => x.DispatchOnly).Returns(true); // default group is dispatch-only

			var otherGroup = new Mock<IChannelGroup>().Object;
			mockFactory.Setup(x => x.Build(Connectors[0], otherConfig.Object)).Returns(otherGroup);

			host.Initialize();
		};

		Because of = () =>
			group = host.GetOutboundChannel(defaultGroupName);

		It should_return_the_correct_instance = () =>
			ReferenceEquals(group, mockGroup.Object).ShouldBeTrue();

		static IOutboundChannel group;
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_disposing_the_host : with_the_messaging_host
	{
		Establish context = () =>
		{
			var outboundConfig = new Mock<IChannelGroupConfiguration>();
			outboundConfig.Setup(x => x.GroupName).Returns("dispatch-only group");
			mockConfigs.Add(outboundConfig);

			outboundGroup.Setup(x => x.DispatchOnly).Returns(true);
			mockFactory.Setup(x => x.Build(Connectors[0], outboundConfig.Object)).Returns(outboundGroup.Object);

			mockGroup.Setup(x => x.Dispose());
			host.Initialize();
		};

		Because of = () =>
			host.Dispose();

		It should_dispose_each_underlying_inbound_channel_group = () =>
			mockGroup.Verify(x => x.Dispose(), Times.Once());

		It should_dispose_each_underlying_outbound_channel_group = () =>
			outboundGroup.Verify(x => x.Dispose(), Times.Once());

		It shouold_dispose_each_of_the_underlying_connectors = () =>
			mockConnectors.ToList().ForEach(mock => mock.Verify(x => x.Dispose(), Times.Once()));

		static readonly Mock<IChannelGroup> outboundGroup = new Mock<IChannelGroup>();
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
		protected static IList<IChannelConnector> Connectors
		{
			get { return mockConnectors == null ? null : mockConnectors.Select(x => x.Object).ToList(); }
		}

		Establish context = () =>
		{
			channelFactory = null;
			defaultGroupName = "Test Configuration Group";
			mockConfig = new Mock<IChannelGroupConfiguration>();
			mockGroup = new Mock<IChannelGroup>();
			
			mockConfig.Setup(x => x.GroupName).Returns(defaultGroupName);
			mockConfigs = new List<Mock<IChannelGroupConfiguration>> { mockConfig };

			mockConnectors = new List<Mock<IChannelConnector>> { new Mock<IChannelConnector>() };
			mockConnectors[0].Setup(x => x.ChannelGroups).Returns(mockConfigs.Select(x => x.Object));

			mockFactory = new Mock<DefaultChannelGroupFactory>();
			mockFactory.Setup(x => x.Build(Connectors[0], mockConfig.Object)).Returns(mockGroup.Object);

			EmptyFactory(null, null); // excericse the code that isn't getting touched

			RebuildHost();
		};
		protected static void RebuildHost()
		{
			if (channelFactory == null && mockFactory != null)
				channelFactory = (c, cfg) => mockFactory.Object.Build(c, cfg);

			host = new DefaultMessagingHost(Connectors, channelFactory);
		}

		protected static readonly ChannelGroupFactory EmptyFactory = (c, cg) => null;
		protected static IList<Mock<IChannelConnector>> mockConnectors;
		protected static Mock<DefaultChannelGroupFactory> mockFactory;
		protected static string defaultGroupName;
		protected static IList<Mock<IChannelGroupConfiguration>> mockConfigs;
		protected static Mock<IChannelGroup> mockGroup;
		protected static DefaultMessagingHost host;
		static ChannelGroupFactory channelFactory;
		static Mock<IChannelGroupConfiguration> mockConfig;
		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169