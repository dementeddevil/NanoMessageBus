#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Machine.Specifications;

	[Subject(typeof(ChannelMessage))]
	public class when_a_channel_message_is_constructed : using_a_channel_message
	{
		It should_contain_the_message_identifier_provided = () =>
			message.MessageId.ShouldEqual(MessageId);

		It should_contain_the_correlation_identifier_provided = () =>
			message.CorrelationId.ShouldEqual(CorrelationId);

		It should_contain_the_return_address_provided = () =>
			message.ReturnAddress.ShouldEqual(ReturnAddress);

		It should_contain_the_set_of_headers_provided = () =>
			message.Headers.ShouldEqual(Headers);

		It should_contain_each_of_the_logical_messages_provided = () =>
			message.Messages.SequenceEqual(Messages).ShouldBeTrue();

		It should_have_a_null_active_message = () =>
			message.ActiveMessage.ShouldBeNull();

		It should_have_a_negative_active_message_index = () =>
			message.ActiveIndex.ShouldEqual(-1);

		It should_not_contain_an_expiration = () =>
			message.Expiration.ShouldEqual(DateTime.MinValue);

		It should_not_be_persistent = () =>
			message.Persistent.ShouldBeFalse();

		It should_not_be_considered_dispatched = () =>
			message.Dispatched.ShouldEqual(DateTime.MinValue);
	}

	[Subject(typeof(ChannelMessage))]
	public class when_attempting_to_modify_the_set_of_logical_messages : using_a_channel_message
	{
		Because of = () =>
			Try(() => message.Messages.Clear());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<NotSupportedException>();
	}

	[Subject(typeof(ChannelMessage))]
	public class when_attempting_to_replace_a_logical_message : using_a_channel_message
	{
		Because of = () =>
			Try(() => message.Messages[0] = null);

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<NotSupportedException>();
	}

	[Subject(typeof(ChannelMessage))]
	public class when_requesting_the_next_message : using_a_channel_message
	{
		Because of = () =>
			result = message.MoveNext();

		It should_set_increment_the_active_message_index = () =>
			message.ActiveMessage.ShouldEqual(Messages[0]);

		It should_set_the_active_message_to_the_message_as_the_position_of_the_active_index = () =>
			message.ActiveIndex.ShouldEqual(0);

		It should_return_true = () =>
			result.ShouldBeTrue();

		static bool result;
	}

	[Subject(typeof(ChannelMessage))]
	public class when_no_more_messages_are_available : using_a_channel_message
	{
		Because of = () =>
		{
			while ((result = message.MoveNext()) == true) { }
		};

		It should_set_the_active_message_to_null = () =>
			message.ActiveMessage.ShouldBeNull();

		It should_set_the_index_to_a_negative_number = () =>
			message.ActiveIndex.ShouldEqual(-1);

		It should_return_false = () =>
			result.ShouldBeFalse();

		static bool result;
	}

	public abstract class using_a_channel_message
	{
		Establish context = () =>
		{
			thrown = null;
			message = new ChannelMessage(
				MessageId,
				CorrelationId,
				ReturnAddress,
				Headers,
				Messages);
		};
		protected static void Try(Action action)
		{
			thrown = Catch.Exception(action);
		}

		protected static Exception thrown;
		protected static ChannelMessage message;
		protected static readonly Guid MessageId = Guid.NewGuid();
		protected static readonly Guid CorrelationId = Guid.NewGuid();
		protected static readonly Uri ReturnAddress = new Uri("http://google.com/");
		protected static readonly Dictionary<string, string> Headers = new Dictionary<string, string>();
		protected static readonly object[] Messages = new object[] { 1, "2", 3.0M, 4.0, 5L };
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169