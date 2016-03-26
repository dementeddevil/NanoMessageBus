﻿#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Machine.Specifications;
    using Moq;
    using It = Machine.Specifications.It;
    using FluentAssertions;

    [Subject(typeof(DefaultDispatchContext))]
	public class when_a_null_message_is_provided : with_a_dispatch_context
	{
		Because of = () =>
			Try(() => dispatchContext.WithMessage(null));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ArgumentNullException>();

		It should_NOT_increment_the_message_count = () =>
			dispatchContext.MessageCount.Should().Be(0);
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_a_null_set_of_message_is_provided : with_a_dispatch_context
	{
		Because of = () =>
			Try(() => dispatchContext.WithMessages(null));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ArgumentNullException>();

		It should_NOT_increment_the_message_count = () =>
			dispatchContext.MessageCount.Should().Be(0);
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_an_empty_set_of_message_is_provided : with_a_dispatch_context
	{
		Because of = () =>
			Try(() => dispatchContext.WithMessages());

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ArgumentException>();

		It should_NOT_increment_the_message_count = () =>
			dispatchContext.MessageCount.Should().Be(0);
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_a_null_header_key_is_provided : with_a_dispatch_context
	{
		Because of = () =>
			Try(() => dispatchContext.WithHeader(null));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ArgumentNullException>();

		It should_NOT_increment_the_header_count = () =>
			dispatchContext.HeaderCount.Should().Be(0);
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_a_null_set_of_headers_is_provided : with_a_dispatch_context
	{
		Because of = () =>
			Try(() => dispatchContext.WithHeaders(null));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ArgumentNullException>();

		It should_NOT_increment_the_header_count = () =>
			dispatchContext.HeaderCount.Should().Be(0);
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_a_null_recipient_is_provided : with_a_dispatch_context
	{
		Because of = () =>
			Try(() => dispatchContext.WithRecipient(null));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_adding_a_message : with_a_dispatch_context
	{
		Because of = () =>
			returnedContext = dispatchContext.WithMessage(0);

		It should_return_an_instance_of_itself = () =>
			returnedContext.Should().Be(dispatchContext);

		It should_increment_the_message_count = () =>
			returnedContext.MessageCount.Should().Be(1);
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_dispatching_an_added_message : with_a_dispatch_context
	{
		Establish context = () =>
			dispatchContext.WithMessage("Hello, World!");

		Because of = () =>
			dispatchContext.Send();

		It should_add_the_message_provided_to_to_the_dispatched_message = () =>
			messages[0].Should().Be("Hello, World!");

		It should_default_to_a_persistent_channel_message = () =>
			message.Persistent.Should().BeTrue();

		It should_reset_the_message_count_to_zero = () =>
			dispatchContext.MessageCount.Should().Be(0);
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_adding_a_set_of_messages : with_a_dispatch_context
	{
		Because of = () =>
			returnedContext = dispatchContext.WithMessages(1, 2, 3, 4);

		It should_return_an_instance_of_itself = () =>
			returnedContext.Should().Be(dispatchContext);

		It should_increment_message_count_by_the_number_of_added_messages = () =>
			returnedContext.MessageCount.Should().Be(4);
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_adding_a_channel_message : with_a_dispatch_context
	{
		Because of = () =>
			returnedContext = dispatchContext.WithMessage(new Mock<ChannelMessage>().Object);

		It should_return_an_instance_of_the_channel_message_dispatch_context = () =>
			returnedContext.Should().BeOfType<DefaultChannelMessageDispatchContext>();
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_dispatching_a_set_of_added_messages : with_a_dispatch_context
	{
		Establish context = () =>
			dispatchContext.WithMessages(null, "Hello, World!", 42, null);

		Because of = () =>
			dispatchContext.Send();

		It should_add_each_message_provided_to_the_dispatched_message = () =>
		{
			messages[0].Should().Be("Hello, World!");
			messages[1].Should().Be(42);
		};

		It should_not_add_any_null_messages_to_the_dispatched_message = () =>
			messages.Length.Should().Be(2);

		It should_reset_the_message_count_to_zero = () =>
			dispatchContext.MessageCount.Should().Be(0);
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_adding_a_correlation_identifier : with_a_dispatch_context
	{
		Because of = () =>
			returnedContext = dispatchContext.WithCorrelationId(Guid.Empty);

		It should_return_an_instance_of_itself = () =>
			returnedContext.Should().Be(dispatchContext);
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_dispatching_a_set_of_messages_with_a_correlation_identifier : with_a_dispatch_context
	{
		Establish context = () =>
			dispatchContext.WithMessage(0).WithCorrelationId(correlationId);

		Because of = () =>
			dispatchContext.Send();

		It should_add_correlation_identifier_dispatched_message = () =>
			message.CorrelationId.Should().Be(correlationId);

		static readonly Guid correlationId = Guid.NewGuid();
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_adding_a_message_header : with_a_dispatch_context
	{
		Because of = () =>
			returnedContext = dispatchContext.WithHeader(string.Empty, "value");

		It should_return_an_instance_of_itself = () =>
			returnedContext.Should().Be(dispatchContext);

		It should_increment_the_header_count = () =>
			dispatchContext.HeaderCount.Should().Be(1);
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_dispatching_a_message_with_a_specified_header : with_a_dispatch_context
	{
		Because of = () =>
			dispatchContext.WithMessage(0).WithHeader("my-key", "Hello, World!").Send();

		It should_append_the_header_to_the_set_of_headers_on_the_dispatched_message = () =>
			headers["my-key"].Should().Be("Hello, World!");

		It should_reset_the_header_count = () =>
			dispatchContext.HeaderCount.Should().Be(0);
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_adding_a_null_message_header_value : with_a_dispatch_context
	{
		Establish context = () =>
			dispatchContext.WithMessage(0).WithHeader("my-key", "Hello, World!");

		Because of = () =>
			dispatchContext.WithHeader("my-key").Send();

		It should_remove_the_header_from_the_set_of_headers_on_the_dispatched_message = () =>
			headers.ContainsKey("my-key").Should().BeFalse();
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_adding_a_set_of_message_headers : with_a_dispatch_context
	{
		Because of = () =>
			returnedContext = dispatchContext.WithHeaders(added);

		It should_return_an_instance_of_itself = () =>
			returnedContext.HeaderCount.Should().Be(3);

		It should_increment_the_header_count_by_the_number_of_headers_added = () =>
			returnedContext.Should().Be(dispatchContext);

		static readonly IDictionary<string, string> added = new Dictionary<string, string>
		{
			{ "1", "1" },
			{ "2", "2" },
			{ "3", "3" }
		};
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_dispatching_a_message_with_a_set_of_headers : with_a_dispatch_context
	{
		Establish context = () =>
		{
			var headers = new Dictionary<string, string>();
			headers["a"] = "1";
			headers["b"] = "2";

			dispatchContext.WithMessage(0).WithHeaders(headers);
		};

		Because of = () =>
			dispatchContext.Send();

		It should_append_the_header_to_the_set_of_headers_on_the_dispatched_message = () =>
		{
			headers["a"].Should().Be("1");
			headers["b"].Should().Be("2");
		};

		It should_reset_the_header_count_to_zero = () =>
			dispatchContext.HeaderCount.Should().Be(0);
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_specifying_the_same_header_twice : with_a_dispatch_context
	{
		Establish context = () =>
			dispatchContext.WithMessage(0).WithHeader("key", "first");

		Because of = () =>
			dispatchContext.WithHeader("key", "last").Send();

		It should_overwrite_any_existing_value_on_the_headers_for_the_same_key_on_the_dispatched_message = () =>
			headers["key"].Should().Be("last");
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_adding_a_recipient : with_a_dispatch_context
	{
		Because of = () =>
			returnedContext = dispatchContext.WithRecipient(ChannelEnvelope.LoopbackAddress);

		It should_return_an_instance_of_itself = () =>
			returnedContext.Should().Be(dispatchContext);
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_dispatching_a_message_with_a_set_of_recipients : with_a_dispatch_context
	{
		Establish context = () =>
			dispatchContext.WithMessage(0);

		Because of = () => dispatchContext
			.WithRecipient(new Uri("http://www.google.com/"))
			.WithRecipient(new Uri("http://www.yahoo.com/"))
			.Send();

		It should_dispatch_the_message_to_each_recipient = () =>
		{
			recipients[0].Should().Be(new Uri("http://www.google.com/"));
			recipients[1].Should().Be(new Uri("http://www.yahoo.com/"));
		};
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_attempting_to_specify_null_state : with_a_dispatch_context
	{
		Because of = () =>
			Try(() => dispatchContext.WithState(null));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_adding_state_to_the_context : with_a_dispatch_context
	{
		Because of = () =>
			dispatchContext.WithMessage(0).WithState("Hello, World!").Send();

		It should_append_the_state_to_the_dispatched_envelope = () =>
			envelope.State.Should().Be("Hello, World!");
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_dispatching_without_first_adding_a_message : with_a_dispatch_context
	{
		Because of = () =>
			Try(() => dispatchContext.Send());

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_no_state_has_been_specified_during_message_delivery : with_a_dispatch_context
	{
		Because of = () =>
			dispatchContext.WithMessage(string.Empty).Send();

		It should_append_the_incoming_message_to_the_dispatch_envelope_state = () =>
			envelope.State.Should().Be(mockMessage.Object);
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_sending_a_message : with_a_dispatch_context
	{
		Establish context = () =>
		{
			mockDispatchTable
				.Setup(x => x[typeof(int)])
				.Returns(new[] { new Uri("http://recipient") });

			dispatchContext
				.WithMessage(0)
				.WithState(state)
				.WithMessage(string.Empty);
		};

		Because of = () =>
			transaction = dispatchContext.WithRecipient(new Uri("http://added")).Send();

		It should_query_the_dispatch_table_with_the_primary_message_type = () =>
			mockDispatchTable.Verify(x => x[typeof(int)], Times.Once());

		It should_send_the_dispatched_message_to_the_recipients_on_the_dispatch_table = () =>
			recipients[0].Should().Be(new Uri("http://recipient"));

		It should_also_send_the_dispatched_message_to_any_added_recipients = () =>
			recipients[1].Should().Be(new Uri("http://added"));

		It should_set_the_return_address_to_the_configured_value = () =>
			message.ReturnAddress.Should().Be(mockDelivery.Object.CurrentConfiguration.ReturnAddress);

		It should_append_the_state_to_the_dispatched_envelope = () =>
			envelope.State.Should().Be(state);

		It should_return_the_current_transaction = () =>
			transaction.Should().Be(mockTransaction.Object);

		static readonly Guid state = Guid.NewGuid();
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_supplying_messages_during_send : with_a_dispatch_context
	{
		Establish context = () =>
			dispatchContext.Send(0, 1, 2, 3);

		It should_append_each_message_to_the_dispatched_envelope = () =>
			envelope.Message.Messages.SequenceEqual(new object[] { 0, 1, 2, 3 }).Should().BeTrue();
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_supplying_messages_during_publish : with_a_dispatch_context
	{
		Establish context = () =>
			dispatchContext.Publish(0, 1, 2, 3);

		It should_append_each_message_to_the_dispatched_envelope = () =>
			envelope.Message.Messages.SequenceEqual(new object[] { 0, 1, 2, 3 }).Should().BeTrue();
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_supplying_messages_during_reply : with_a_dispatch_context
	{
		Establish context = () =>
			dispatchContext.Reply(0, 1, 2, 3);

		It should_append_each_message_to_the_dispatched_envelope = () =>
			envelope.Message.Messages.SequenceEqual(new object[] { 0, 1, 2, 3 }).Should().BeTrue();
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_sending_with_the_same_context_more_than_once : with_a_dispatch_context
	{
		Establish context = () =>
			dispatchContext.WithMessage(0).Send();

		Because of = () =>
			Try(() => dispatchContext.Send());

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_publishing_a_message : with_a_dispatch_context
	{
		Establish context = () =>
		{
			mockDispatchTable
				.Setup(x => x[typeof(int)])
				.Returns(new[] { new Uri("http://recipient") });

			dispatchContext.WithMessage(0).WithMessage(string.Empty);
		};

		Because of = () =>
			transaction = dispatchContext.WithRecipient(new Uri("http://added")).Publish();

		It should_query_the_dispatch_table_with_the_primary_message_type = () =>
			mockDispatchTable.Verify(x => x[typeof(int)], Times.Once());

		It should_publish_the_dispatched_message_to_the_recipients_on_the_dispatch_table = () =>
			recipients[0].Should().Be(new Uri("http://recipient"));

		It should_also_publish_the_dispatched_message_to_any_added_recipients = () =>
			recipients[1].Should().Be(new Uri("http://added"));

		It should_set_the_return_address_to_the_configured_value = () =>
			message.ReturnAddress.Should().Be(mockDelivery.Object.CurrentConfiguration.ReturnAddress);

		It should_return_the_current_transaction = () =>
			transaction.Should().Be(mockTransaction.Object);
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_publishing_with_the_same_context_more_than_once : with_a_dispatch_context
	{
		Establish context = () =>
			dispatchContext.WithMessage(0).Publish();

		Because of = () =>
			Try(() => dispatchContext.Publish());

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_replying_to_a_message : with_a_dispatch_context
	{
		Establish context = () =>
		{
			mockMessage.Setup(x => x.CorrelationId).Returns(incomingCorrelationId);

			dispatchContext.WithMessage(0);
		};

		Because of = () =>
			transaction = dispatchContext.Reply();

		It should_NOT_query_the_dispatch_table_with_the_primary_message_type = () =>
			mockDispatchTable.Verify(x => x[typeof(int)], Times.Never());

		It should_set_the_return_address_to_the_configured_value = () =>
			message.ReturnAddress.Should().Be(mockDelivery.Object.CurrentConfiguration.ReturnAddress);

		It should_send_the_reply_message_to_incoming_message_return_address = () =>
			recipients[0].Should().Be(IncomingReturnAddress);

		It should_use_the_correlation_identifier_from_the_incoming_message = () =>
			message.CorrelationId.Should().Be(incomingCorrelationId);

		It should_return_the_current_transaction = () =>
			transaction.Should().Be(mockTransaction.Object);

		static readonly Guid incomingCorrelationId = Guid.NewGuid();
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_replying_to_a_null_incoming_message : with_a_dispatch_context
	{
		Establish context = () =>
			mockDelivery.Setup(x => x.CurrentMessage).Returns((ChannelMessage)null);

		Because of = () =>
			Try(() => dispatchContext.Reply());

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_replying_to_a_message_but_using_a_specific_correlation_identifier : with_a_dispatch_context
	{
		Establish context = () =>
			dispatchContext.WithMessage(0).WithCorrelationId(specifiedCorrelationId);

		Because of = () =>
			dispatchContext.Reply();

		It should_use_the_correlation_identifier_specified = () =>
			message.CorrelationId.Should().Be(specifiedCorrelationId);

		static readonly Guid specifiedCorrelationId = Guid.NewGuid();
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_replying_with_the_same_context_more_than_once : with_a_dispatch_context
	{
		Establish context = () =>
			dispatchContext.WithMessage(0).Reply();

		Because of = () =>
			Try(() => dispatchContext.Reply());

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_no_messages_are_provided_upon_send : with_a_dispatch_context
	{
		Because of = () =>
			Try(() => dispatchContext.Send());

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_no_messages_are_provided_upon_publish : with_a_dispatch_context
	{
		Because of = () =>
			Try(() => dispatchContext.Publish());

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_no_messages_are_provided_upon_reply : with_a_dispatch_context
	{
		Because of = () =>
			Try(() => dispatchContext.Reply());

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_no_recipients_are_listening_to_the_sent_message : with_a_dispatch_context
	{
		Establish context = () =>
			dispatchContext.WithMessage(0);

		Because of = () =>
			dispatchContext.Send();

		It should_send_the_message_to_the_dead_letter_address = () =>
			recipients[0].Should().Be(ChannelEnvelope.UnroutableMessageAddress);
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_no_recipients_are_listening_to_the_published_message : with_a_dispatch_context
	{
		Establish context = () =>
			dispatchContext.WithMessage(0);

		Because of = () =>
			dispatchContext.Publish();

		It should_send_the_message_to_the_dead_letter_address = () =>
			recipients[0].Should().Be(ChannelEnvelope.UnroutableMessageAddress);
	}

	[Subject(typeof(DefaultDispatchContext))]
	public class when_replying_to_a_message_with_no_return_address : with_a_dispatch_context
	{
		Establish context = () =>
		{
			mockMessage.Setup(x => x.ReturnAddress).Returns((Uri)null);
			dispatchContext.WithMessage(0);
		};

		Because of = () =>
			dispatchContext.Reply();

		It should_send_the_message_to_the_dead_letter_address = () =>
			recipients[0].Should().Be(ChannelEnvelope.UnroutableMessageAddress);
	}

	public abstract class with_a_dispatch_context
	{
		Establish context = () =>
		{
			mockDispatchTable = new Mock<IDispatchTable>();

			mockConfig = new Mock<IChannelGroupConfiguration>();
			mockConfig.Setup(x => x.ReturnAddress).Returns(OutgoingReturnAddress);
			mockConfig.Setup(x => x.MessageBuilder).Returns(new DefaultChannelMessageBuilder());
			mockConfig.Setup(x => x.DispatchTable).Returns(mockDispatchTable.Object);

			mockMessage = new Mock<ChannelMessage>();
			mockMessage.Setup(x => x.ReturnAddress).Returns(IncomingReturnAddress);

			mockTransaction = new Mock<IChannelTransaction>();

			mockDelivery = new Mock<IMessagingChannel>();
			mockDelivery.Setup(x => x.CurrentMessage).Returns(mockMessage.Object);
			mockDelivery.Setup(x => x.CurrentConfiguration).Returns(mockConfig.Object);
			mockDelivery.Setup(x => x.CurrentTransaction).Returns(mockTransaction.Object);

			mockDelivery.Setup(x => x.SendAsync(Moq.It.IsAny<ChannelEnvelope>())).Callback<ChannelEnvelope>(x =>
			{
				envelope = x;
				message = envelope.Message;
				messages = message.Messages.ToArray();
				headers = message.Headers;
				recipients = envelope.Recipients.ToArray();
			});

			envelope = null;
			message = null;
			messages = null;
			headers = null;
			recipients = null;
			thrown = null;
			transaction = null;

			dispatchContext = new DefaultDispatchContext(mockDelivery.Object);
		};
		protected static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		protected static readonly Uri IncomingReturnAddress = new Uri("http://incoming-return-address/");
		protected static readonly Uri OutgoingReturnAddress = new Uri("http://outgoing-return-address/");
		protected static DefaultDispatchContext dispatchContext;
		protected static IDispatchContext returnedContext;
		protected static Mock<IMessagingChannel> mockDelivery;
		protected static Mock<IChannelTransaction> mockTransaction;
		protected static Mock<IDispatchTable> mockDispatchTable;
		protected static Mock<IChannelGroupConfiguration> mockConfig;
		protected static Mock<ChannelMessage> mockMessage;
		protected static ChannelEnvelope envelope;
		protected static ChannelMessage message;
		protected static object[] messages;
		protected static IDictionary<string, string> headers;
		protected static Uri[] recipients;
		protected static IChannelTransaction transaction;
		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169, 414