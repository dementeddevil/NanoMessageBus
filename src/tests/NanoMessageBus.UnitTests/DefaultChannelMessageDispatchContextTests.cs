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

	[Subject(typeof(DefaultChannelMessageDispatchContext))]
	public class when_a_null_channel_is_provided_during_construction : using_a_channel_message_dispatch_context
	{
		Because of = () =>
			Try(() => Build(null, message));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultChannelMessageDispatchContext))]
	public class when_a_null_channel_message_is_provided_during_construction : using_a_channel_message_dispatch_context
	{
		Because of = () =>
			Try(() => Build(mockChannel.Object, null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultChannelMessageDispatchContext))]
	public class when_the_dispatch_context_is_constructed : using_a_channel_message_dispatch_context
	{
		It should_indicate_a_single_message = () =>
			dispatchContext.MessageCount.ShouldEqual(1);

		It should_indicate_zero_headers = () =>
			dispatchContext.HeaderCount.ShouldEqual(0);
	}

	[Subject(typeof(DefaultChannelMessageDispatchContext))]
	public class when_attempting_to_add_a_message : using_a_channel_message_dispatch_context
	{
		Because of = () =>
			Try(() => dispatchContext.WithMessage(0));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultChannelMessageDispatchContext))]
	public class when_attempting_to_add_a_set_of_messages : using_a_channel_message_dispatch_context
	{
		Because of = () =>
			Try(() => dispatchContext.WithMessages(0));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultChannelMessageDispatchContext))]
	public class when_attempting_to_set_the_correlation_identifier : using_a_channel_message_dispatch_context
	{
		Because of = () =>
			Try(() => dispatchContext.WithCorrelationId(Guid.Empty));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultChannelMessageDispatchContext))]
	public class when_attempting_to_set_a_header : using_a_channel_message_dispatch_context
	{
		Because of = () =>
			Try(() => dispatchContext.WithHeader(string.Empty));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultChannelMessageDispatchContext))]
	public class when_attempting_to_add_multiple_headers : using_a_channel_message_dispatch_context
	{
		Because of = () =>
			Try(() => dispatchContext.WithHeaders(new Dictionary<string, string>()));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultChannelMessageDispatchContext))]
	public class when_adding_a_null_recipient : using_a_channel_message_dispatch_context
	{
		Because of = () =>
			Try(() => dispatchContext.WithRecipient(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultChannelMessageDispatchContext))]
	public class when_attempting_to_add_a_recipient : using_a_channel_message_dispatch_context
	{
		Because of = () =>
			returnedContext = dispatchContext.WithRecipient(ChannelEnvelope.LoopbackAddress);

		It should_NOT_throw_an_exception = () =>
			thrown.ShouldBeNull();

		It should_return_reference_to_itself = () =>
			returnedContext.ShouldEqual(dispatchContext);

		static IDispatchContext returnedContext;
	}

	[Subject(typeof(DefaultChannelMessageDispatchContext))]
	public class when_publishing_the_dispatch : using_a_channel_message_dispatch_context
	{
		Because of = () =>
			Try(() => dispatchContext.Publish());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultChannelMessageDispatchContext))]
	public class when_replying_the_dispatch : using_a_channel_message_dispatch_context
	{
		Because of = () =>
			Try(() => dispatchContext.Reply());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultChannelMessageDispatchContext))]
	public class when_sending_the_dispatch : using_a_channel_message_dispatch_context
	{
		Establish context = () =>
			recipients.ToList().ForEach(x => dispatchContext.WithRecipient(x));

		Because of = () =>
			transaction = dispatchContext.Send();

		It should_send_the_message_through_the_underlying_channel = () =>
			envelope.Message.ShouldEqual(message);

		It should_send_append_the_recipients_to_the_envelope = () =>
			envelope.Recipients.SequenceEqual(recipients).ShouldBeTrue();

		It should_a_reference_to_the_underlying_transaction = () =>
			transaction.ShouldEqual(mockTransaction.Object);

		It should_report_the_number_of_messages_as_zero = () =>
			dispatchContext.MessageCount.ShouldEqual(0);

		static IChannelTransaction transaction;
		static readonly Uri[] recipients = new[] { new Uri("http://first"), new Uri("http://second") };
	}

	[Subject(typeof(DefaultChannelMessageDispatchContext))]
	public class when_sending_the_dispatch_multiple_times : using_a_channel_message_dispatch_context
	{
		Establish context = () =>
			dispatchContext.WithRecipient(new Uri("http://first")).Send();

		Because of = () =>
			Try(() => dispatchContext.Send());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	public abstract class using_a_channel_message_dispatch_context
	{
		Establish context = () =>
		{
			thrown = null;
			envelope = null;

			mockChannel = new Mock<IMessagingChannel>();
			mockTransaction = new Mock<IChannelTransaction>();
			mockChannel.Setup(x => x.CurrentTransaction).Returns(mockTransaction.Object);

			mockChannel
				.Setup(x => x.Send(Moq.It.IsAny<ChannelEnvelope>()))
				.Callback<ChannelEnvelope>(x => envelope = x);

			Build(mockChannel.Object, message);
		};
		protected static void Build(IMessagingChannel channel, ChannelMessage channelMessage)
		{
			dispatchContext = new DefaultChannelMessageDispatchContext(channel, channelMessage);
		}
		protected static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		protected static DefaultChannelMessageDispatchContext dispatchContext;
		protected static Mock<IMessagingChannel> mockChannel;
		protected static Mock<IChannelTransaction> mockTransaction;
		protected static ChannelMessage message = new Mock<ChannelMessage>().Object;
		protected static ChannelEnvelope envelope;
		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169