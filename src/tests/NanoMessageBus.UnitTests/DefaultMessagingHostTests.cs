#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
	using Machine.Specifications;

	[Subject(typeof(DefaultMessagingHost))]
	public class when_initializing_the_host
	{
		Establish context = () => { };
		Because of = () => { };
		It should_obtain_a_list_of_channel_groups_from_each_underlying_connector = () => { };
		It should_call_the_channel_group_factory_for_each_channel_group = () => { };
		It should_provide_the_channel_group_factory_with_each_connector = () => { };
		It should_provide_the_channel_group_factory_with_a_named_channel_group_belonging_to_that_connector = () => { };
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_initializing_the_host_more_than_once
	{
		Establish context = () => { };
		Because of = () => { };
		It should_do_nothing = () => { };
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_initializing_a_disposed_host
	{
		Establish context = () => { };
		Because of = () => { };
		It should_throw_an_exception = () => { };
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_instructed_to_begin_receiving_messages
	{
		Establish context = () => { };
		Because of = () => { };
		It should_pass_the_callback_to_the_underlying_connection_groups = () => { };
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_instructed_to_begin_receiving_messages_without_providing_a_callback
	{
		Establish context = () => { };
		Because of = () => { };
		It should_throw_an_exception = () => { };
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_attempting_to_begin_receiving_messages_without_first_initializing_the_host
	{
		Establish context = () => { };
		Because of = () => { };
		It should_throw_an_exception = () => { };
	}

	[Subject(typeof(DefaultMessagingHost))]
	public class when_attempting_to_receive_messages_against_a_disposed_host
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
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169