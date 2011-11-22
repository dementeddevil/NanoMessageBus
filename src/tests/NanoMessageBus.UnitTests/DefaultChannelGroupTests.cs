#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
	using System;
	using System.Threading;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject(typeof(DefaultChannelGroup))]
	public class when_constructing_a_new_channel_group : with_a_channel_group
	{
		Establish context = () =>
			mockConfig.Setup(x => x.DispatchOnly).Returns(true);

		It should_contain_the_same_dispatch_mode_as_the_configuration = () =>
			channelGroup.DispatchOnly.ShouldBeTrue();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_asynchronously_dispatching_a_message_to_a_dispatch_only_group : with_a_channel_group
	{
		Establish context = () =>
		{
			mockConfig.Setup(x => x.DispatchOnly).Returns(true);
			mockChannel.Setup(x => x.Send(envelope));
			channelGroup.Initialize();
		};

		Because of = () =>
			channelGroup.BeginDispatch(envelope, trx => { });

		Because of_multi_threading = () =>
			Thread.Sleep(100);

		It should_pass_the_message_to_exactly_one_of_the_underlying_channels = () =>
			mockChannel.Verify(x => x.Send(envelope), Times.Once());
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_asynchronously_dispatching_to_a_full_duplex_group : with_a_channel_group
	{
		static Exception thrown;

		Establish context = () =>
			channelGroup.Initialize();

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.BeginDispatch(envelope, trx => { }));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_no_message_is_provided_to_asynchronously_dispatch : with_a_channel_group
	{
		static Exception thrown;

		Establish context = () =>
			channelGroup.Initialize();

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.BeginDispatch(null, trx => { }));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_no_completion_callback_is_provided_for_asynchronous_dispatch : with_a_channel_group
	{
		static Exception thrown;

		Establish context = () =>
			channelGroup.Initialize();

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.BeginDispatch(envelope, null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_attempting_to_asynchronously_dispatching_a_message_without_first_initializing_the_group : with_a_channel_group
	{
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.BeginDispatch(envelope, trx => { }));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_asynchronously_dispatching_against_a_disposed_group : with_a_channel_group
	{
		static Exception thrown;

		Establish context = () =>
		{
			channelGroup.Initialize();
			channelGroup.Dispose();
		};

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.BeginDispatch(envelope, trx => { }));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();
	}

	[Ignore("TODO")]
	[Subject(typeof(DefaultChannelGroup))]
	public class when_attempting_to_receive_messages : with_a_channel_group
	{
		static readonly Action<IDeliveryContext> callback = context => { };

		Establish context = () =>
			channelGroup.Initialize();

		Because of = () =>
			channelGroup.BeginReceive(callback);

		It should_pass_the_callback_to_the_underlying_channel;
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_attempting_to_receive_messages_without_providing_a_callback : with_a_channel_group
	{
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.BeginReceive(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_attempting_to_begin_receiving_messages_without_first_initializing_the_group : with_a_channel_group
	{
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.BeginReceive(c => { }));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_attempting_to_receive_messages_against_a_disposed_group : with_a_channel_group
	{
		static Exception thrown;

		Establish context = () =>
		{
			channelGroup.Initialize();
			channelGroup.Dispose();
		};

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.BeginReceive(c => { }));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_more_than_one_callback_has_been_provided_for_receiving_messages_from_the_group : with_a_channel_group
	{
		static readonly Action<IDeliveryContext> callback = channel => { };
		static Exception thrown;

		Establish context = () =>
		{
			channelGroup.Initialize();
			channelGroup.BeginReceive(callback);
		};

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.BeginReceive(callback));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_attempting_to_receive_from_a_dispatch_only_channel_group : with_a_channel_group
	{
		static readonly Action<IDeliveryContext> callback = channel => { };
		static Exception thrown;

		Establish context = () =>
		{
			mockConfig.Setup(x => x.DispatchOnly).Returns(true);
			channelGroup.Initialize();
		};

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.BeginReceive(callback));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	public abstract class with_a_channel_group
	{
		protected const string ChannelGroupName = "Test Channel Group";
		protected static DefaultChannelGroup channelGroup;
		protected static Mock<IChannelConnector> mockConnector;
		protected static Mock<IChannelConfiguration> mockConfig;
		protected static ChannelEnvelope envelope;
		protected static Mock<IMessagingChannel> mockChannel;

		Establish context = () =>
		{
			mockConnector = new Mock<IChannelConnector>();
			mockChannel = new Mock<IMessagingChannel>();
			mockConfig = new Mock<IChannelConfiguration>();
			envelope = new Mock<ChannelEnvelope>().Object;

			mockConfig.Setup(x => x.ChannelGroup).Returns(ChannelGroupName);
			mockConnector.Setup(x => x.Connect(ChannelGroupName)).Returns(mockChannel.Object);

			channelGroup = new DefaultChannelGroup(mockConnector.Object, mockConfig.Object);
		};

		Cleanup after = () =>
		{
			channelGroup.Dispose();

			mockConnector = null;
			mockConfig = null;
			envelope = null;
		};
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169