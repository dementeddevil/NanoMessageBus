using FluentAssertions;

#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
	using System;
	using System.Configuration;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject(typeof(SynchronousChannelGroup))]
	public class when_a_null_connector_is_provided_during_constructing : using_a_synchronous_channel_group
	{
		Because of = () =>
			Try(() => new SynchronousChannelGroup(null, mockConfiguration.Object));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ArgumentNullException>();
	}

	[Subject(typeof(SynchronousChannelGroup))]
	public class when_a_null_configuration_is_provided_during_constructing : using_a_synchronous_channel_group
	{
		Because of = () =>
			Try(() => new SynchronousChannelGroup(mockConnector.Object, null));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ArgumentNullException>();
	}

	[Subject(typeof(SynchronousChannelGroup))]
	public class when_the_channel_group_has_been_constructed : using_a_synchronous_channel_group
	{
		Establish context = () =>
			mockConfiguration.Setup(x => x.DispatchOnly).Returns(true);

		It should_expose_the_dispatch_only_status_from_the_underlying_configuration = () =>
			channelGroup.DispatchOnly.Should().Be(mockConfiguration.Object.DispatchOnly);
	}

	[Subject(typeof(SynchronousChannelGroup))]
	public class when_initialize_is_called : using_a_synchronous_channel_group
	{
		Because of = () =>
			channelGroup.Initialize();

		It should_not_do_anything = () =>
			thrown.Should().BeNull();
	}

	[Subject(typeof(SynchronousChannelGroup))]
	public class when_initialize_is_called_multiple_times : using_a_synchronous_channel_group
	{
		Because of = () =>
			channelGroup.Initialize();

		It should_not_throw_an_exception = () =>
			thrown.Should().BeNull();
	}

	[Subject(typeof(SynchronousChannelGroup))]
	public class when_initialize_is_called_after_the_group_has_been_disposed : using_a_synchronous_channel_group
	{
		Establish context = () =>
			channelGroup.Dispose();

		Because of = () =>
			Try(() => channelGroup.Initialize());

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(SynchronousChannelGroup))]
	public class when_attempting_to_asynchronously_receive : using_a_synchronous_channel_group
	{
		Because of = () =>
			Try(() => channelGroup.BeginReceive(null));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<NotSupportedException>();
	}

	[Subject(typeof(SynchronousChannelGroup))]
	public class when_attempting_to_asynchronously_dispatch : using_a_synchronous_channel_group
	{
		Because of = () =>
			Try(() => channelGroup.BeginDispatch(null));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<NotSupportedException>();
	}

	[Subject(typeof(SynchronousChannelGroup))]
	public class when_opening_a_channel_on_an_uninitialized_group : using_a_synchronous_channel_group
	{
		Because of = () =>
			Try(() => channelGroup.OpenChannel());

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<InvalidOperationException>();
	}

	[Subject(typeof(SynchronousChannelGroup))]
	public class when_opening_a_channel_on_a_disposed_group : using_a_synchronous_channel_group
	{
		Establish context = () =>
			channelGroup.Dispose();

		Because of = () =>
			Try(() => channelGroup.OpenChannel());

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(SynchronousChannelGroup))]
	public class when_opening_a_channel : using_a_synchronous_channel_group
	{
		Establish context = () =>
		{
			mockChannel = new Mock<IMessagingChannel>();
			mockConnector.Setup(x => x.Connect(ChannelGroupName)).Returns(mockChannel.Object);

			channelGroup.Initialize();
		};

		Because of = () =>
			opened = channelGroup.OpenChannel();

		It should_attempt_to_open_a_channel_on_the_underlying_connector = () =>
			mockConnector.Verify(x => x.Connect(ChannelGroupName));

		It should_return_a_reference_to_the_opened_channel = () =>
			opened.Should().Be(mockChannel.Object);

		static Mock<IMessagingChannel> mockChannel;
		static IMessagingChannel opened;
	}

	[Subject(typeof(SynchronousChannelGroup))]
	public class when_the_channel_connector_throws_an_exception : using_a_synchronous_channel_group
	{
		Establish context = () =>
		{
			mockConnector.Setup(x => x.Connect(ChannelGroupName)).Throws(new ConfigurationErrorsException());
			channelGroup.Initialize();
		};

		Because of = () =>
			Try(() => channelGroup.OpenChannel());

		It should_not_catch_the_exception = () =>
			thrown.Should().BeOfType<ConfigurationErrorsException>();
	}

	public abstract class using_a_synchronous_channel_group
	{
		Establish context = () =>
		{
			mockConfiguration = new Mock<IChannelGroupConfiguration>();
			mockConfiguration.Setup(x => x.GroupName).Returns(ChannelGroupName);

			mockConnector = new Mock<IChannelConnector>();
			mockConnector.Setup(x => x.ChannelGroups).Returns(new[] { mockConfiguration.Object });

			thrown = null;

			channelGroup = new SynchronousChannelGroup(mockConnector.Object, mockConfiguration.Object);
		};

		protected static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		protected const string ChannelGroupName = "Sync Group";
		protected static SynchronousChannelGroup channelGroup;
		protected static Mock<IChannelGroupConfiguration> mockConfiguration;
		protected static Mock<IChannelConnector> mockConnector;
		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169, 414