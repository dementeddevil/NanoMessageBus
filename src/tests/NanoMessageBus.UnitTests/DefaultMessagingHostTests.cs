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
			thrown = Catch.Exception(() => new DefaultMessagingHost(null, EmptyChannelGroupFactory));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_an_empty_set_of_connectors_is_provided_during_construction : with_the_messaging_host
	{
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() =>
				new DefaultMessagingHost(EmptyChannelConnectors, EmptyChannelGroupFactory));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_no_channel_group_factory_is_provided_during_construction : with_the_messaging_host
	{
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => new DefaultMessagingHost(EmptyChannelConnectors, null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_initializing_the_host
	{
		static readonly List<Mock<IChannelConnector>> connectors = new List<Mock<IChannelConnector>>();
		static readonly Mock<IChannelConfiguration> config0 = new Mock<IChannelConfiguration>();
		static readonly Mock<IChannelConfiguration> config1 = new Mock<IChannelConfiguration>();
		static readonly Mock<IChannelConfiguration> config2 = new Mock<IChannelConfiguration>();

		static readonly Mock<DefaultChannelGroupFactory> factory = new Mock<DefaultChannelGroupFactory>();
		static IMessagingHost host;

		Establish context = () =>
		{
			connectors.Add(new Mock<IChannelConnector>());
			connectors[0].SetupGet(x => x.ChannelGroups).Returns(new[] { config0.Object, config1.Object });

			connectors.Add(new Mock<IChannelConnector>());
			connectors[1].SetupGet(x => x.ChannelGroups).Returns(new[] { config2.Object });

			config0.SetupGet(x => x.ChannelGroup).Returns("config0");
			config1.SetupGet(x => x.ChannelGroup).Returns("config1");
			config2.SetupGet(x => x.ChannelGroup).Returns("config2");

			factory.Setup(x => x.Build(connectors[0].Object, config0.Object));
			factory.Setup(x => x.Build(connectors[0].Object, config1.Object));
			factory.Setup(x => x.Build(connectors[1].Object, config2.Object));

			host = new DefaultMessagingHost(connectors.Select(x => x.Object), factory.Object.Build);
		};

		Because of = () =>
		    host.Initialize();

		It should_obtain_a_list_of_channel_groups_from_each_underlying_connector = () =>
			connectors.ForEach(x => x.VerifyGet(mock => mock.ChannelGroups));
			
		It should_provide_each_config_and_its_associated_connector_to_the_factory = () =>
			factory.VerifyAll();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_initializing_the_host_more_than_once
	{
		static readonly List<Mock<IChannelConnector>> connectors = new List<Mock<IChannelConnector>>();
		static readonly Mock<IChannelConfiguration> config0 = new Mock<IChannelConfiguration>();

		static readonly Mock<DefaultChannelGroupFactory> factory = new Mock<DefaultChannelGroupFactory>();
		static IMessagingHost host;

		Establish context = () =>
		{
			connectors.Add(new Mock<IChannelConnector>());
			connectors[0].SetupGet(x => x.ChannelGroups).Returns(new[] { config0.Object });
			config0.SetupGet(x => x.ChannelGroup).Returns("config0");
			factory.Setup(x => x.Build(connectors[0].Object, config0.Object));

			host = new DefaultMessagingHost(connectors.Select(x => x.Object), factory.Object.Build);
		};

		private Because of = () =>
		{
			host.Initialize();
			host.Initialize();
			host.Initialize();
		};

		It should_do_nothing = () =>
			factory.Verify(x => x.Build(connectors[0].Object, config0.Object), Times.Once());
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_initializing_a_disposed_host : with_the_messaging_host
	{
		static readonly IMessagingHost host = new DefaultMessagingHost(
			PopulatedChannelConnectors, EmptyChannelGroupFactory);
		static Exception thrown;

		Establish context = () =>
			host.Dispose();

		Because of = () =>
			thrown = Catch.Exception(() => host.Initialize());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_instructed_to_begin_receiving_messages
	{
		Establish context = () => { };
		Because of = () => { };
		It should_pass_the_callback_to_the_underlying_connection_groups = () => { };
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_instructed_to_begin_receiving_messages_without_providing_a_callback : with_the_messaging_host
	{
		static readonly IMessagingHost host = new DefaultMessagingHost(
			PopulatedChannelConnectors, EmptyChannelGroupFactory);
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => host.BeginReceive(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_attempting_to_begin_receiving_messages_without_first_initializing_the_host : with_the_messaging_host
	{
		static readonly IMessagingHost host = new DefaultMessagingHost(
			PopulatedChannelConnectors, EmptyChannelGroupFactory);
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => host.BeginReceive(c => { }));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_attempting_to_receive_messages_against_a_disposed_host : with_the_messaging_host
	{
		static readonly IMessagingHost host = new DefaultMessagingHost(
			PopulatedChannelConnectors, EmptyChannelGroupFactory);
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
	public class when_a_new_callback_is_provided_for_receiving_messages
	{
		Establish context = () => { };
		Because of = () => { };
		It should_throw_an_exception = () => { };
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_asynchronously_dispatching_a_message
	{
		Establish context = () => { };
		Because of = () => { };
		It should_pass_the_message_to_the_specified_channel_group = () => { };
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_synchronously_dispatching_a_message
	{
		Establish context = () => { };
		Because of = () => { };
		It should_pass_the_message_to_the_specified_channel_group = () => { };
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_no_message_is_provided_to_asynchronously_dispatch
	{
		Establish context = () => { };
		Because of = () => { };
		It should_throw_an_exception = () => { };
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_no_message_is_provided_to_synchronously_dispatch
	{
		Establish context = () => { };
		Because of = () => { };
		It should_throw_an_exception = () => { };
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_no_channel_group_is_specified_for_asynchronous_dispatch
	{
		Establish context = () => { };
		Because of = () => { };
		It should_throw_an_exception = () => { };
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_no_channel_group_is_specified_for_synchronous_dispatch
	{
		Establish context = () => { };
		Because of = () => { };
		It should_throw_an_exception = () => { };
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_attempting_to_asynchronously_dispatching_a_message_without_first_initializing_the_host
	{
		Establish context = () => { };
		Because of = () => { };
		It should_throw_an_exception = () => { };
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_attempting_to_synchronously_dispatching_a_message_without_first_initializing_the_host
	{
		Establish context = () => { };
		Because of = () => { };
		It should_throw_an_exception = () => { };
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_the_channel_group_specified_for_asynchronous_dispatch_doesnt_exist
	{
		Establish context = () => { };
		Because of = () => { };
		It should_throw_an_exception = () => { };
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_the_channel_group_specified_for_synchronous_dispatch_doesnt_exist
	{
		Establish context = () => { };
		Because of = () => { };
		It should_throw_an_exception = () => { };
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_asynchronously_dispatching_against_a_disposed_host
	{
		Establish context = () => { };
		Because of = () => { };
		It should_throw_an_exception = () => { };
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_synchronously_dispatching_against_a_disposed_host
	{
		Establish context = () => { };
		Because of = () => { };
		It should_throw_an_exception = () => { };
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_disposing_the_host
	{
		Establish context = () => { };
		Because of = () => { };
		It should_dispose_each_underlying_channel_group = () => { };
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_disposing_the_host_more_than_once
	{
		Establish context = () => { };
		Because of = () => { };
		It should_do_nothing = () => { };
	}

	public abstract class with_the_messaging_host
	{
		protected static readonly ChannelGroupFactory EmptyChannelGroupFactory = (c, cg) => null;
		protected static readonly IEnumerable<IChannelConnector> EmptyChannelConnectors
			= new IChannelConnector[0];
		protected static readonly IList<IChannelConnector> PopulatedChannelConnectors = new[]
		{
			new Mock<IChannelConnector>().Object,
			new Mock<IChannelConnector>().Object
		};
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169