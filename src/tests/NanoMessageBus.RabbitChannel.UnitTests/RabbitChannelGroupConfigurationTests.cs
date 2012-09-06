#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using Machine.Specifications;
	using Moq;
	using RabbitMQ.Client;
	using Serialization;
	using It = Machine.Specifications.It;

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_creating_channel_group_configuration : using_channel_config
	{
		It should_default_to_dispatch_only = () =>
			config.DispatchOnly.ShouldBeTrue();

		It should_default_to_asynchronous_operations = () =>
			config.Synchronous.ShouldBeFalse();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_specifying_the_channel_group_name : using_channel_config
	{
		Because of = () =>
			config.WithGroupName("some name");

		It should_contain_the_name_specified = () =>
			config.GroupName.ShouldEqual("some name");
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_a_null_channel_group_name_is_specified : using_channel_config
	{
		Because of = () =>
			thrown = Catch.Exception(() => config.WithGroupName(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_no_group_name_is_specified : using_channel_config
	{
		It should_contain_the_default_value = () =>
			config.GroupName.ShouldEqual("unnamed-group");
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_no_group_name_is_specified_on_a_duplex_channel : using_channel_config
	{
		Because of = () =>
			config = config.WithInputQueue("my-queue");

		It should_contain_the_input_queue_name = () =>
			config.GroupName.ShouldEqual("my-queue");
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_a_group_name_is_specified_on_a_duplex_channel : using_channel_config
	{
		Because of = () =>
			config = config.WithInputQueue("my-queue").WithGroupName("my-group");

		It should_contain_the_group_name_specified = () =>
			config.GroupName.ShouldEqual("my-group");
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_specifying_synchronous_operations : using_channel_config
	{
		Because of = () =>
			config.WithSynchronousOperation();

		It should_indicate_operations_to_be_synchronous = () =>
			config.Synchronous.ShouldBeTrue();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_clustered_queues_are_not_specified : using_channel_config
	{
		It should_default_to_single_node_queues = () =>
			config.Clustered.ShouldBeFalse();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_clustered_queues_are_requested : using_channel_config
	{
		Because of = () =>
			config.WithInputQueue("some-queue", clustered: true);

		It should_indicate_replicated_queues_will_be_created = () =>
			config.Clustered.ShouldBeTrue();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_configuring_with_clustering : using_channel_config
	{
		Establish context = () =>
		{
			mockChannel
				.Setup(x => x.QueueDeclare(
					"my queue",
					Moq.It.IsAny<bool>(),
					Moq.It.IsAny<bool>(),
					Moq.It.IsAny<bool>(),
					Moq.It.IsAny<IDictionary>()))
				.Callback<string, bool, bool, bool, IDictionary>((a, b, c, e, values) =>
				{
					parameters = values;
				});

			config.WithInputQueue("my queue", clustered: true);
		};

		Because of = () => Configure();

		It should_provide_the_parameter_to_the_queue_declaration = () =>
			parameters["x-ha-policy"].ShouldEqual("all");

		static IDictionary parameters;
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_specifying_the_receipt_timeout : using_channel_config
	{
		Because of = () =>
			config.WithReceiveTimeout(TimeSpan.FromSeconds(1));

		It should_contain_the_timeout_specified = () =>
			config.ReceiveTimeout.ShouldEqual(TimeSpan.FromSeconds(1));
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_specifying_a_negative_receipt_timeout : using_channel_config
	{
		Because of = () =>
			thrown = Catch.Exception(() => config.WithReceiveTimeout(TimeSpan.FromSeconds(-1)));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_no_receipt_timeout_is_specified : using_channel_config
	{
		It should_default_to_1500_milliseconds = () =>
			config.ReceiveTimeout.ShouldEqual(TimeSpan.FromMilliseconds(1500));
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_specifying_the_min_and_max_workers_values : using_channel_config
	{
		Because of = () =>
			config.WithWorkers(5, 10);

		It should_contain_the_min_value_specified = () =>
			config.MinWorkers.ShouldEqual(5);

		It should_contain_the_max_value_specified = () =>
			config.MaxWorkers.ShouldEqual(10);
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_no_min_or_max_workers_values_are_specified : using_channel_config
	{
		It should_contain_the_default_min_value = () =>
			config.MinWorkers.ShouldEqual(1);

		It should_contain_the_default_max_value = () =>
			config.MaxWorkers.ShouldEqual(1);
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_specifying_a_higher_min_workers_than_the_max_workers : using_channel_config
	{
		Because of = () =>
			thrown = Catch.Exception(() => config.WithWorkers(42, 3));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_specifying_a_non_positive_min_worker_count : using_channel_config
	{
		Because of = () =>
			thrown = Catch.Exception(() => config.WithWorkers(0, 1));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_specifying_the_input_queue : using_channel_config
	{
		Establish context = () =>
			config.WithDispatchOnly();

		Because of = () =>
			config.WithInputQueue("My.Queue");

		It should_contain_the_lower_case_variant_of_the_queue_name_specified_with_periods_replaced = () =>
			config.InputQueue.ShouldEqual("my-queue");

		It should_be_full_duplex = () =>
			config.DispatchOnly.ShouldBeFalse();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_no_input_queue_name_is_provided : using_channel_config
	{
		Because of = () =>
			thrown = Catch.Exception(() => config.WithInputQueue(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_specifying_an_auto_generate_queue : using_channel_config
	{
		Establish context = () =>
			config.WithInputQueue("some-queue"); // this should get overwritten

		Because of = () =>
			config.WithRandomInputQueue();

		It should_contain_an_empty_queue_name = () =>
			config.InputQueue.ShouldBeEmpty();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_configuring_the_underlying_channel_with_a_random_queue_name : using_channel_config
	{
		Establish context = () =>
		{
			var declaration = new QueueDeclareOk("random-queue", 0, 0);

			mockChannel
				.Setup(x => x.QueueDeclare(string.Empty, true, false, AutoDelete, Moq.It.IsAny<IDictionary>()))
				.Returns(declaration);

			config.WithRandomInputQueue();
		};

		Because of = () =>
			Configure();

		It should_append_a_random_name_to_the_configuration = () =>
			config.InputQueue.ShouldEqual("random-queue");

		It should_set_the_return_address_to_the_random_name = () =>
			config.ReturnAddress.ToString().ShouldEqual("direct://default/random-queue");

		const bool AutoDelete = true;
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_specifying_a_channel_group_as_dispatch_only : using_channel_config
	{
		Establish context = () =>
			config.WithInputQueue("some-queue");

		Because of = () =>
			config.WithDispatchOnly();

		It should_be_marked_as_dispatch_only = () =>
			config.DispatchOnly.ShouldBeTrue();

		It should_remove_the_configured_input_queue_name = () =>
			config.InputQueue.ShouldBeNull();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_no_dispatch_buffer_size_is_specified : using_channel_config
	{
		It should_not_limited_the_outbound_buffer_size = () =>
			config.MaxDispatchBuffer.ShouldEqual(int.MaxValue);
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_a_non_positive_dispatch_buffer_size_is_specified : using_channel_config
	{
		Because of = () =>
			Try(() => config.WithMaxDispatchBuffer(0));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentOutOfRangeException>();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_a_dispatch_buffer_size_is_specified : using_channel_config
	{
		Because of = () =>
			config.WithMaxDispatchBuffer(42);

		It should_be_marked_on_the_configuration = () =>
			config.MaxDispatchBuffer.ShouldEqual(42);
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_no_transaction_type_has_been_configured : using_channel_config
	{
		It should_default_to_full_transactions = () =>
			config.TransactionType.ShouldEqual(RabbitTransactionType.Full);
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_transactions_are_not_desired : using_channel_config
	{
		Because of = () =>
			config.WithTransaction(RabbitTransactionType.None);

		It should_not_use_transactions = () =>
			config.TransactionType.ShouldEqual(RabbitTransactionType.None);
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_message_acknowledgement_is_specified : using_channel_config
	{
		Because of = () =>
			config.WithTransaction(RabbitTransactionType.Acknowledge);

		It should_enlist_in_acknowledge_based_transactions = () =>
			config.TransactionType.ShouldEqual(RabbitTransactionType.Acknowledge);
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_full_transactions_are_specified : using_channel_config
	{
		Because of = () =>
			config.WithTransaction(RabbitTransactionType.Full);

		It should_enlist_in_full_transactions = () =>
			config.TransactionType.ShouldEqual(RabbitTransactionType.Full);
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_a_channel_buffer_is_specified : using_channel_config
	{
		Because of = () =>
			config.WithChannelBuffer(42);

		It configure_the_channel_QOS_with_the_buffer_size_provided = () =>
			config.ChannelBuffer.ShouldEqual(42);
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_no_channel_buffer_is_specified : using_channel_config
	{
		It should_contain_the_default_value = () =>
			config.ChannelBuffer.ShouldEqual(1024);
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_the_channel_buffer_is_specified_is_negative : using_channel_config
	{
		Because of = () =>
			thrown = Catch.Exception(() => config.WithChannelBuffer(-1));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_the_channel_buffer_is_beyond_65535_messages : using_channel_config
	{
		Because of = () =>
			config.WithChannelBuffer(ushort.MaxValue + 1);

		It should_truncate_the_value_to_a_maximum_of_65535 = () =>
			config.ChannelBuffer.ShouldEqual(ushort.MaxValue);
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_no_channel_message_builder_is_specified : using_channel_config
	{
		It should_return_the_default_channel_message_builder = () =>
			config.MessageBuilder.ShouldBeOfType<DefaultChannelMessageBuilder>();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_a_null_channel_message_builder_instance_is_specified : using_channel_config
	{
		Because of = () =>
			thrown = Catch.Exception(() => config.WithChannelMessageBuilder(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_a_channel_message_builder_instance_is_specified : using_channel_config
	{
		Because of = () =>
			config.WithChannelMessageBuilder(builder);

		It should_return_the_instance_specified = () =>
			config.MessageBuilder.ShouldEqual(builder);

		static readonly IChannelMessageBuilder builder = new Mock<IChannelMessageBuilder>().Object;
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_no_return_address_is_specified_on_dispatch_only_endpoints : using_channel_config
	{
		It should_not_have_a_return_address = () =>
			config.ReturnAddress.ShouldBeNull();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_no_return_address_is_specified_on_full_duplex_endpoints : using_channel_config
	{
		Because of = () =>
			config.WithInputQueue("my-queue-name");

		It should_use_the_input_queue_name_specified = () =>
			config.ReturnAddress.ToString().ShouldEqual("direct://default/my-queue-name");
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_a_return_address_specified : using_channel_config
	{
		Because of = () =>
			config.WithReturnAddress(address);

		It should_contain_the_address_specified = () =>
			config.ReturnAddress.ShouldEqual(address);
		
		static readonly Uri address = new Uri("direct://some-exchange/some-queue");
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_a_null_return_address_is_specified : using_channel_config
	{
		Because of = () =>
			thrown = Catch.Exception(() => config.WithReturnAddress(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_a_poison_message_exchange_is_specified : using_channel_config
	{
		Because of = () =>
			config.WithPoisonMessageExchange("my-exchange");

		It should_contain_the_exchange_specified = () =>
			config.PoisonMessageExchange.ExchangeName.ShouldEqual("my-exchange");

		It should_be_a_fanout_exchange = () =>
			config.PoisonMessageExchange.ExchangeType.ShouldEqual(ExchangeType.Fanout);
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_no_poison_message_exchange_is_specified : using_channel_config
	{
		It should_point_to_the_default_poison_message_exchange = () =>
			config.PoisonMessageExchange.ToString().ShouldEqual("fanout://poison-messages/");
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_a_null_poison_message_exchange_is_specified : using_channel_config
	{
		Because of = () =>
			thrown = Catch.Exception(() => config.WithPoisonMessageExchange(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_an_empty_poison_message_exchange_is_specified : using_channel_config
	{
		Because of = () =>
			config.WithPoisonMessageExchange(string.Empty);

		It should_no_longer_point_to_any_poison_message_exchange = () =>
			config.PoisonMessageExchange.ShouldBeNull();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_a_dead_letter_exchange_is_specified : using_channel_config
	{
		Because of = () =>
			config.WithDeadLetterExchange("my-exchange");

		It should_contain_the_exchange_specified = () =>
			config.DeadLetterExchange.ExchangeName.ShouldEqual("my-exchange");

		It should_be_a_fanout_exchange = () =>
			config.DeadLetterExchange.ExchangeType.ShouldEqual(ExchangeType.Fanout);
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_no_dead_letter_exchange_is_specified : using_channel_config
	{
		It should_point_to_the_default_exchange = () =>
			config.DeadLetterExchange.ToString().ShouldEqual("fanout://dead-letters/");
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_a_null_dead_letter_exchange_is_specified : using_channel_config
	{
		Because of = () =>
			thrown = Catch.Exception(() => config.WithDeadLetterExchange(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_an_empty_dead_letter_exchange_is_specified : using_channel_config
	{
		Because of = () =>
			config.WithDeadLetterExchange(string.Empty);

		It should_no_longer_point_to_any_dead_letter_exchange = () =>
			config.DeadLetterExchange.ShouldBeNull();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_specifying_the_maximum_number_of_receive_attempts : using_channel_config
	{
		Because of = () =>
			config.WithMaxAttempts(42);

		It should_contain_the_value_specified = () =>
			config.MaxAttempts.ShouldEqual(42);
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_maximum_number_of_receive_attempts_is_not_positive : using_channel_config
	{
		Because of = () =>
			thrown = Catch.Exception(() => config.WithMaxAttempts(-1));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_maximum_number_of_receive_attempts_is_not_specified : using_channel_config
	{
		It should_contain_the_default_number_of_attempts = () =>
			config.MaxAttempts.ShouldEqual(3);
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_specifying_an_application_id : using_channel_config
	{
		Because of = () =>
			config.WithApplicationId(" myapp ");

		It should_contain_the_trimmed_value_specified = () =>
			config.ApplicationId.ShouldEqual("myapp");
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_a_null_application_identifier_is_provided : using_channel_config
	{
		Because of = () =>
			thrown = Catch.Exception(() => config.WithApplicationId(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_an_application_identifier_is_not_specified : using_channel_config
	{
		It should_contain_the_default_value = () =>
			config.ApplicationId.ShouldEqual("rabbit-endpoint");
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_getting_the_message_adapter : using_channel_config
	{
		Because of = () =>
			messageAdapter = config.MessageAdapter;

		It should_return_a_single_instance_for_a_given_channel_group = () =>
			ReferenceEquals(messageAdapter, config.MessageAdapter).ShouldBeTrue();

		It should_be_unique_from_another_channel_group = () =>
			ReferenceEquals(messageAdapter, new RabbitChannelGroupConfiguration().MessageAdapter).ShouldBeFalse();

		static object messageAdapter;
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_a_serializer_is_specified : using_channel_config
	{
		Because of = () =>
			config.WithSerializer(serializer);

		It should_contain_the_value_specified = () =>
			ReferenceEquals(config.Serializer, serializer).ShouldBeTrue();

		private static readonly ISerializer serializer = new Mock<ISerializer>().Object;
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_a_null_serializer_is_specified : using_channel_config
	{
		Because of = () =>
			thrown = Catch.Exception(() => config.WithSerializer(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_no_serializer_is_specified : using_channel_config
	{
		It should_contain_the_default_serializer = () =>
			config.Serializer.ShouldBeOfType<BinarySerializer>();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_configuring_a_new_receive_channel : using_channel_config
	{
		Establish context = () => config
			.WithInputQueue("some-queue")
			.WithPoisonMessageExchange("poison-msgs")
			.WithDeadLetterExchange("dead-letters");

		Because of = () => Configure();

		It should_set_the_QOS_on_the_channel = () =>
			mockChannel.Verify(x => x.BasicQos(0, (ushort)config.ChannelBuffer, false), Times.Once());

		It should_declare_the_poison_message_exchange = () =>
			mockChannel.Verify(x => x.ExchangeDeclare("poison-msgs", ExchangeType.Fanout, true, false, null));

		It should_declare_a_poison_message_queue = () =>
			mockChannel.Verify(x => x.QueueDeclare("poison-msgs", true, false, false, null));

		It should_bind_poison_message_exchange_and_queue = () =>
			mockChannel.Verify(x => x.QueueBind("poison-msgs", "poison-msgs", string.Empty, null));

		It should_declare_the_dead_letter_exchange_if_specified = () =>
			mockChannel.Verify(x => x.ExchangeDeclare("dead-letters", ExchangeType.Fanout, true, false, null));

		It should_declare_a_dead_letter_queue_if_specified = () =>
			mockChannel.Verify(x => x.QueueDeclare("dead-letters", true, false, false, null));

		It should_bind_dead_letter_exchange_and_queue = () =>
			mockChannel.Verify(x => x.QueueBind("dead-letters", "dead-letters", string.Empty, null));
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_configuring_the_channel_with_a_return_address_different_than_the_queue : using_channel_config
	{
		Establish context = () =>
		{
			config.WithReturnAddress(new Uri("direct://default/ReturnAddress"));
			config.WithInputQueue("some-queue");
		};

		Because of = () => Configure();

		It should_maintain_the_same_return_address_specified = () =>
			config.ReturnAddress.ToString().ShouldEqual("direct://default/ReturnAddress");
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_specifying_named_input_queue_exclusivity : using_channel_config
	{
		Establish context = () =>
			config.WithInputQueue("some-queue").WithExclusiveReceive();

		Because of = () => Configure();

		It should_build_the_channel_with_the_named_exclusive_value_specified = () =>
			mockChannel.Verify(x => x.QueueDeclare("some-queue", true, true, true, Moq.It.IsAny<IDictionary>()), Times.Once());
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_specifying_exclusive_receive_without_an_input_queue : using_channel_config
	{
		Establish context = () =>
			config.WithExclusiveReceive();

		Because of = () => Configure();

		It should_build_the_channel_with_the_randomly_named_exclusive_queue = () =>
			mockChannel.Verify(x => x.QueueDeclare(string.Empty, true, true, true, Moq.It.IsAny<IDictionary>()), Times.Once());
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_specifying_named_input_queue_durability : using_channel_config
	{
		Establish context = () =>
			config.WithInputQueue("some-queue").WithTransientQueue();

		Because of = () => Configure();

		It should_build_the_channel_with_the_exclusive_value_specified = () =>
			mockChannel.Verify(x => x.QueueDeclare("some-queue", false, false, false, Moq.It.IsAny<IDictionary>()), Times.Once());
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_specifying_unnamed_input_queue_durability : using_channel_config
	{
		Establish context = () =>
			config.WithTransientQueue();

		Because of = () => Configure();

		It should_build_the_channel_with_the_exclusive_value_specified = () =>
			mockChannel.Verify(x => x.QueueDeclare(string.Empty, false, false, false, Moq.It.IsAny<IDictionary>()), Times.Once());
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_specifying_a_named_queue_as_auto_delete : using_channel_config
	{
		Establish context = () =>
			config.WithInputQueue("some-queue").WithAutoDeleteQueue();

		Because of = () => Configure();

		It should_build_the_channel_with_the_autodelete_value_specified = () =>
			mockChannel.Verify(x => x.QueueDeclare("some-queue", true, false, true, Moq.It.IsAny<IDictionary>()), Times.Once());
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_specifying_an_unnamed_queue_as_auto_delete : using_channel_config
	{
		Establish context = () =>
			config.WithAutoDeleteQueue();

		Because of = () => Configure();

		It should_build_the_channel_with_the_autodelete_value_specified = () =>
			mockChannel.Verify(x => x.QueueDeclare(string.Empty, true, false, true, Moq.It.IsAny<IDictionary>()), Times.Once());
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_marking_the_queue_to_be_purged_at_startup : using_channel_config
	{
		Establish context = () =>
			config.WithInputQueue("my queue").WithCleanQueue();

		Because of = () => Configure();

		It should_invoke_purge_when_initializing_the_connection = () =>
			mockChannel.Verify(x => x.QueuePurge(config.InputQueue), Times.Once());
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_specifying_a_poison_message_exchange : using_channel_config
	{
		Establish context = () =>
		{
			mockChannel
				.Setup(x => x.QueueDeclare(
					"my queue",
					Moq.It.IsAny<bool>(),
					Moq.It.IsAny<bool>(),
					Moq.It.IsAny<bool>(),
					Moq.It.IsAny<IDictionary>()))
				.Callback<string, bool, bool, bool, IDictionary>((a, b, c, e, values) =>
				{
					parameters = values;
				});

			config.WithInputQueue("my queue").WithDeadLetterExchange("my-dead-letter-exchange");
		};

		Because of = () => Configure();

		It should_provide_the_parameter_to_the_queue_declaration = () =>
			parameters["x-dead-letter-exchange"].ShouldEqual("my-dead-letter-exchange");

		static IDictionary parameters;
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_marking_a_dispatch_only_config_to_purge_at_startup : using_channel_config
	{
		Because of = () => Configure();

		It should_NOT_invoke_purge_when_initializing_the_connection = () =>
			mockChannel.Verify(x => x.QueuePurge(config.InputQueue), Times.Never());
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_looking_up_a_routing_key : using_channel_config
	{
		Establish context = () =>
		{
			mockMessage = new Mock<ChannelMessage>();
			mockMessage.Setup(x => x.Messages).Returns(logicalMessages);
		};

		Because of = () =>
			routingKey = config.LookupRoutingKey(mockMessage.Object);

		It should_use_the_lowercase_type_name_of_the_first_logical_message_in_the_ChannelMessage = () =>
			routingKey.ShouldEqual("system-string");

		static readonly object[] logicalMessages = new object[] { "1", 2, 3.0, 4.0M, "5", (ushort)6 };
		static Mock<ChannelMessage> mockMessage;
		static string routingKey;
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_providing_a_set_of_message_types : using_channel_config
	{
		Establish context = () =>
			config.WithMessageTypes(types);

		Because of = () =>
			Configure();

		It should_declare_a_durable_fanout_exchange_for_each_type = () =>
			types.ToList().ForEach(type => mockChannel.Verify(model =>
				model.ExchangeDeclare(
					type.FullName.NormalizeName(), ExchangeType.Fanout, true, false, null), Times.Once()));

		static readonly IEnumerable<Type> types =
			new object[] { "1", 2, 3.0, 4.0M, "5", (ushort)6 }.Select(x => x.GetType());
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_providing_a_set_of_message_types_on_an_input_queue : using_channel_config
	{
		Establish context = () =>
			config.WithMessageTypes(types).WithInputQueue("Some-Queue");

		Because of = () =>
			Configure();

		It should_bind_the_queue_to_the_declared_exchanges = () =>
			types.ToList().ForEach(type => mockChannel.Verify(model =>
				model.QueueBind("some-queue", type.FullName.NormalizeName(), string.Empty, null), Times.Once()));

		static readonly IEnumerable<Type> types =
			new object[] { "1", 2, 3.0, 4.0M, "5", (ushort)6 }.Select(x => x.GetType());
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_multiple_sets_of_messages_types_have_been_specified : using_channel_config
	{
		Establish context = () =>
			config.WithMessageTypes(types.Take(3)).WithInputQueue("queue");

		Because of = () =>
		{
			config.WithMessageTypes(types.Reverse().Take(3));
			Configure();
		};

		It should_append_all_specified_message_types_to_the_list = () =>
			types.ToList().ForEach(type => mockChannel.Verify(model =>
				model.QueueBind("queue", type.FullName.NormalizeName(), string.Empty, null), Times.Once()));

		static readonly IEnumerable<Type> types =
			new object[] { "1", 2, 3.0, 4.0M, "5", (ushort)6 }.Select(x => x.GetType());
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_a_null_collection_of_message_types_is_provided : using_channel_config
	{
		Because of = () =>
			thrown = Catch.Exception(() => config.WithMessageTypes(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_no_dependency_resolver_has_been_specified : using_channel_config
	{
		It should_not_have_a_resolver = () =>
			config.DependencyResolver.ShouldBeNull();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_a_null_dependency_resolver_is_provided : using_channel_config
	{
		Because of = () =>
			thrown = Catch.Exception(() => config.WithDependencyResolver(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_a_dependency_resolver_is_provided : using_channel_config
	{
		Because of = () =>
			config.WithDependencyResolver(resolver);

		It should_set_the_resolver_reference_on_the_configuration = () =>
			config.DependencyResolver.ShouldEqual(resolver);

		static readonly IDependencyResolver resolver = new Mock<IDependencyResolver>().Object;
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_no_dispatch_table_has_been_provied : using_channel_config
	{
		It should_use_the_default_rabbit_dispatch_table = () =>
			config.DispatchTable.ShouldBeOfType<RabbitDispatchTable>();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_a_null_dispatch_table_is_provied : using_channel_config
	{
		Because of = () =>
			Try(() => config.WithDispatchTable(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(RabbitChannelGroupConfiguration))]
	public class when_a_dispatch_table_is_provied : using_channel_config
	{
		Because of = () =>
			config.WithDispatchTable(mockTable.Object);

		It should_use_the_dispatch_table_provided = () =>
			config.DispatchTable.ShouldEqual(mockTable.Object);

		static readonly Mock<IDispatchTable> mockTable = new Mock<IDispatchTable>();
	}

	public abstract class using_channel_config
	{
		Establish context = () =>
		{
			thrown = null;
			mockChannel = new Mock<IModel>();
			config = new RabbitChannelGroupConfiguration();
		};

		protected static void Configure()
		{
			config.ConfigureChannel(mockChannel.Object);
		}
		protected static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		protected static Mock<IModel> mockChannel;
		protected static RabbitChannelGroupConfiguration config;
		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169