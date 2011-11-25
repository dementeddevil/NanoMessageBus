#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.RabbitChannel
{
	using System;
	using Machine.Specifications;
	using Moq;
	using RabbitMQ.Client.Events;
	using Serialization;
	using It = Machine.Specifications.It;

	[Subject(typeof(RabbitMessageAdapter))]
	public class when_no_message_is_supplied_to_build_a_ChannelMessage_from_a_wire_message : using_a_message_adapter
	{
		Because of = () =>
			thrown = Catch.Exception(() => adapter.Build((BasicDeliverEventArgs)null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(RabbitMessageAdapter))]
	public class when_no_message_is_supplied_to_build_a_wire_message_from_a_ChannelMessage : using_a_message_adapter
	{
		Because of = () =>
			thrown = Catch.Exception(() => adapter.Build((ChannelMessage)null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(RabbitMessageAdapter))]
	public class when_no_message_is_supplied_to_which_an_exception_should_be_appended : using_a_message_adapter
	{
		Because of = () =>
			thrown = Catch.Exception(() => adapter.AppendException(null, new Exception()));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(RabbitMessageAdapter))]
	public class when_no_message_is_supplied_when_purging_the_message_from_the_cache : using_a_message_adapter
	{
		Because of = () =>
			thrown = Catch.Exception(() => adapter.AppendException(null, new Exception()));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(RabbitMessageAdapter))]
	public class when_no_exception_is_supplied_when_appending_an_exception : using_a_message_adapter
	{
		Because of = () =>
			thrown = Catch.Exception(() => adapter.AppendException(null, new Exception()));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(RabbitMessageAdapter))]
	public class when_adding_an_exception_to_the_wire_message : using_a_message_adapter
	{
		Because of = () =>
			adapter.AppendException(message, simple);

		It should_append_the_exception_message = () =>
			message.GetHeader("x-exception0-message").ShouldNotBeNull();

		It should_append_the_exception_type = () =>
			message.GetHeader("x-exception0-type").ShouldNotBeNull();

		It should_append_the_exception_stack_trace = () =>
			message.GetHeader("x-exception0-stacktrace").ShouldNotBeNull();

		static readonly BasicDeliverEventArgs message = new BasicDeliverEventArgs();
		static readonly Exception simple = new Exception();
	}

	[Subject(typeof(RabbitMessageAdapter))]
	public class when_adding_a_nested_exception_to_the_wire_message : using_a_message_adapter
	{
		Because of = () =>
			adapter.AppendException(message, nested);

		It should_append_the_inner_exception_message = () =>
			message.GetHeader("x-exception1-message").ShouldNotBeNull();

		It should_append_the_inner_exception_type = () =>
			message.GetHeader("x-exception1-type").ShouldNotBeNull();

		It should_append_the_inner_exception_stack_trace = () =>
			message.GetHeader("x-exception1-stacktrace").ShouldNotBeNull();

		static readonly BasicDeliverEventArgs message = new BasicDeliverEventArgs();
		static readonly Exception nested = new Exception("outer", new Exception("inner"));
	}

	[Subject(typeof(RabbitMessageAdapter))]
	public class when_adding_a_multi_layer_exception_to_the_wire_message : using_a_message_adapter
	{
		Because of = () =>
			adapter.AppendException(message, multiple);

		It should_append_each_layer_of_the_exception_message = () =>
			message.GetHeader("x-exception2-message").ShouldNotBeNull();

		It should_append_each_layer_of_the_exception_type = () =>
			message.GetHeader("x-exception2-type").ShouldNotBeNull();

		It should_append_each_layer_of_theexception_stack_trace = () =>
			message.GetHeader("x-exception2-stacktrace").ShouldNotBeNull();

		static readonly BasicDeliverEventArgs message = new BasicDeliverEventArgs();
		static readonly Exception multiple = new Exception("0", new Exception("1", new Exception("2")));
	}

	[Subject(typeof(RabbitMessageAdapter))]
	public class when_purging_a_message_which_has_not_been_tracked : using_a_message_adapter
	{
		Because of = () =>
			cached = adapter.PurgeFromCache(new BasicDeliverEventArgs());

		It should_indicate_that_the_message_was_not_found_in_the_cache = () =>
			cached.ShouldBeFalse();

		static bool cached;
	}

	[Subject(typeof(RabbitMessageAdapter))]
	public class when_purging_a_message_which_been_tracked : using_a_message_adapter
	{
		Establish context = () =>
			adapter.Build(message);

		Because of = () =>
			cached = adapter.PurgeFromCache(message);

		It should_indicate_that_the_message_was_removed_from_cached = () =>
			cached.ShouldBeTrue();

		static readonly BasicDeliverEventArgs message = new BasicDeliverEventArgs();
		static bool cached;
	}

	[Subject(typeof(RabbitMessageAdapter))]
	public class when_purging_a_message_twice_from_the_adapter_cache : using_a_message_adapter
	{
		Establish context = () =>
			adapter.Build(message);

		Because of = () =>
		{
			first = adapter.PurgeFromCache(message);
			second = adapter.PurgeFromCache(message);
		};

		It should_first_indicate_that_the_message_was_removed_from_the_cache = () =>
			first.ShouldBeTrue();

		It should_then_indicate_that_the_message_was_not_found_in_the_cache = () =>
			second.ShouldBeFalse();

		static readonly BasicDeliverEventArgs message = new BasicDeliverEventArgs();
		static bool first;
		static bool second;
	}

	public abstract class using_a_message_adapter
	{
		Establish context = () =>
		{
			mockSerializer = new Mock<ISerializer>();
			adapter = new RabbitMessageAdapter(mockSerializer.Object);
		};

		protected static Mock<ISerializer> mockSerializer;
		protected static RabbitMessageAdapter adapter;
		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169