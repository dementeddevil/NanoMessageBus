#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.RabbitChannel.UnitTests
{
	using Machine.Specifications;

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_specifying_the_channel_group_name
	{
		Establish context = () => { };
		Because of = () => { };
		It should_contain_the_name_specified = () => { };
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_no_channel_group_name_is_specified
	{
		Establish context = () => { };
		Because of = () => { };
		It should_throw_an_ArgumentNullException = () => { };
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_specifying_the_receipt_timeout
	{
		Establish context = () => { };
		Because of = () => { };
		It should_contain_the_timeout_specified = () => { };
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_specifying_a_negative_receipt_timeout
	{
		Establish context = () => { };
		Because of = () => { };
		It should_throw_an_ArgumentException = () => { };
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_no_receipt_timeout_is_specified
	{
		Establish context = () => { };
		Because of = () => { };
		It should_default_to_500_milliseconds = () => { };
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_specifying_the_min_and_max_thread_values
	{
		Establish context = () => { };
		Because of = () => { };
		It should_contain_the_min_value_specified = () => { };
		It should_contain_the_max_value_specified = () => { };
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_no_min_or_max_thread_values_are_specified
	{
		Establish context = () => { };
		Because of = () => { };
		It should_contain_the_default_min_value = () => { };
		It should_contain_the_default_max_value = () => { };
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_specifying_a_higher_min_threads_than_the_current_max_thread_count
	{
		Establish context = () => { };
		Because of = () => { };
		It should_contain_the_min_value_specified = () => { };
		It should_update_the_max_value_to_the_new_higher_min_value = () => { };
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_specifying_a_channel_group_as_dispatch_only
	{
		Establish context = () => { };
		Because of = () => { };
		It should_be_marked_as_dispatch_only = () => { };
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_specifying_the_input_queue
	{
		Establish context = () => { };
		Because of = () => { };
		It should_contain_the_queue_name_specified = () => { };
		It should_contain_a_private_exchange_based_upon_the_name = () => { };
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_specifying_an_auto_generate_queue
	{
		Establish context = () => { };
		Because of = () => { };
		It should_contain_an_empty_queue_name = () => { };
		It should_contain_a_guid_based_private_exchange = () => { };
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_no_transaction_type_is_specified
	{
		Establish context = () => { };
		Because of = () => { };
		It should_not_enlist_in_any_transactions = () => { };
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_message_acknowledgement_is_specified
	{
		Establish context = () => { };
		Because of = () => { };
		It should_enlist_in_acknowledge_based_transactions = () => { };
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_full_transactions_are_specified
	{
		Establish context = () => { };
		Because of = () => { };
		It should_enlist_in_full_transactions = () => { };
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_a_channel_buffer_is_specified
	{
		Establish context = () => { };
		Because of = () => { };
		It configure_the_channel_QOS_with_the_buffer_size_provided = () => { };
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_the_channel_buffer_is_specified_is_negative
	{
		Establish context = () => { };
		Because of = () => { };
		It should_throw_an_ArgumentException = () => { };
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_specifying_input_queue_exclusivity
	{
		Establish context = () => { };
		Because of = () => { };
		It should_build_the_channel_with_the_exclusive_value_specified = () => { };
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_marking_the_queue_to_be_purged_at_startup
	{
		Establish context = () => { };
		Because of = () => { };
		It should_invoke_purge_when_initializing_the_connection = () => { };
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_marking_the_queue_for_deletion_at_shutdown
	{
		Establish context = () => { };
		Because of = () => { };
		It should_mark_the_queue_for_deletion_when_initializing_the_connection = () => { };
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_initializing_the_connection
	{
		Establish context = () => { };
		Because of = () => { };
		It should_invoke_the_wireup_callbacks_specified = () => { };
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169