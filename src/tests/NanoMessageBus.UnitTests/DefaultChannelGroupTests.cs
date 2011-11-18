#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
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

	[Ignore("TODO")]
	[Subject(typeof(DefaultChannelGroup))]
	public class when_asynchronously_dispatching_a_message_to_a_dispatch_only_group : with_a_channel_group
	{
		Establish context = () =>
			channelGroup.Initialize();

		Because of = () =>
			channelGroup.BeginDispatch(mockMessage.Object, recipients);

		It should_pass_the_message_to_underlying_connector;
	}

	[Ignore("TODO")]
	[Subject(typeof(DefaultChannelGroup))]
	public class when_synchronously_dispatching_a_message_to_a_dispatch_only_group : with_a_channel_group
	{
		Establish context = () =>
			channelGroup.Initialize();

		Because of = () =>
			channelGroup.Dispatch(mockMessage.Object, recipients);

		It should_pass_the_message_to_underlying_connector;
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_asynchronously_dispatching_to_a_full_duplex_group : with_a_channel_group
	{
		static Exception thrown;

		Establish context = () =>
			channelGroup.Initialize();

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.BeginDispatch(mockMessage.Object, recipients));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_synchronously_dispatching_to_a_full_duplex_group : with_a_channel_group
	{
		static Exception thrown;

		Establish context = () =>
			channelGroup.Initialize();

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.Dispatch(mockMessage.Object, recipients));

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
			thrown = Catch.Exception(() => channelGroup.BeginDispatch(null, recipients));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_no_message_is_provided_to_synchronously_dispatch : with_a_channel_group
	{
		static Exception thrown;

		Establish context = () =>
			channelGroup.Initialize();

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.Dispatch(null, recipients));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_a_null_set_of_recipients_are_specified_for_asynchronous_dispatch : with_a_channel_group
	{
		static Exception thrown;

		Establish context = () =>
			channelGroup.Initialize();

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.BeginDispatch(mockMessage.Object, null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_a_null_set_of_recipients_are_specified_for_synchronous_dispatch : with_a_channel_group
	{
		static Exception thrown;

		Establish context = () =>
			channelGroup.Initialize();

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.Dispatch(mockMessage.Object, null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_no_recipients_are_specified_for_asynchronous_dispatch : with_a_channel_group
	{
		static Exception thrown;

		Establish context = () =>
		{
			recipients.Clear();
			channelGroup.Initialize();
		};

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.BeginDispatch(mockMessage.Object, recipients));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_no_recipients_are_specified_for_synchronous_dispatch : with_a_channel_group
	{
		static Exception thrown;

		Establish context = () =>
		{
			recipients.Clear();
			channelGroup.Initialize();
		};

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.Dispatch(mockMessage.Object, recipients));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_attempting_to_asynchronously_dispatching_a_message_without_first_initializing_the_group : with_a_channel_group
	{
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.BeginDispatch(mockMessage.Object, recipients));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_attempting_to_synchronously_dispatching_a_message_without_first_initializing_the_group : with_a_channel_group
	{
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.Dispatch(mockMessage.Object, recipients));

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
			thrown = Catch.Exception(() => channelGroup.BeginDispatch(mockMessage.Object, recipients));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_synchronously_dispatching_against_a_disposed_group : with_a_channel_group
	{
		static Exception thrown;

		Establish context = () =>
		{
			channelGroup.Initialize();
			channelGroup.Dispose();
		};

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.Dispatch(mockMessage.Object, recipients));

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
		protected static IChannelGroup channelGroup; // todo: rename
		protected static Mock<IChannelConnector> mockConnector;
		protected static Mock<IChannelConfiguration> mockConfig;
		protected static Mock<ChannelMessage> mockMessage;
		protected static ICollection<Uri> recipients;

		Establish context = () =>
		{
			mockConnector = new Mock<IChannelConnector>();
			mockConfig = new Mock<IChannelConfiguration>();

			mockMessage = new Mock<ChannelMessage>();
			recipients = new List<Uri> { new Uri("http://localhost/") };

			channelGroup = new DefaultChannelGroup(mockConnector.Object, mockConfig.Object);
		};

		Cleanup after = () =>
		{
			mockConnector = null;
			mockConfig = null;
			mockMessage = null;
			recipients = null;
		};
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169