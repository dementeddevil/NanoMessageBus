#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.RabbitChannel
{
	using System;
	using System.Runtime.Serialization;
	using Machine.Specifications;
	using Moq;
	using RabbitMQ.Client;
	using RabbitMQ.Client.Events;
	using It = Machine.Specifications.It;

	[Subject(typeof(RabbitChannel))]
	public class when_opening_a_transactional_channel : using_a_channel
	{
		Establish context = () =>
		{
			mockRealChannel.Setup(x => x.TxSelect());
			RequireTransaction(RabbitTransactionType.Full);
		};

		Because of = () =>
			Initialize();

		It should_mark_the_underlying_channel_as_transactional = () =>
			mockRealChannel.Verify(x => x.TxSelect(), Times.Once());
	}

	[Subject(typeof(RabbitChannel))]
	public class when_opening_a_channel_with_a_channel_buffer_size_specified : using_a_channel
	{
		Establish context = () =>
		{
			mockConfiguration.Setup(x => x.ChannelBuffer).Returns(BufferSize);
			mockRealChannel.Setup(x => x.BasicQos(0, BufferSize, false));
		};

		Because of = () =>
			Initialize();

		It should_specify_the_QOS_to_the_underlying_channel = () =>
			mockRealChannel.Verify(x => x.BasicQos(0, BufferSize, false), Times.Once());

		const ushort BufferSize = 42;
	}

	[Subject(typeof(RabbitChannel))]
	public class when_opening_the_channel_to_receive : using_a_channel
	{
		Establish context = () =>
		{
			mockConfiguration.Setup(x => x.ReceiveTimeout).Returns(timeout);

			channel = new RabbitChannel(mockRealChannel.Object, mockAdapter.Object, mockConfiguration.Object, () =>
			{
				invocations++;
				return mockSubscription.Object;
			});
		};

		Because of = () =>
			channel.Receive(delivery => { });

		It should_call_the_subscription_factory = () =>
			invocations.ShouldEqual(1);

		It should_open_the_subscription_to_receive = () =>
			mockSubscription.Verify(x =>
				x.BeginReceive(timeout, Moq.It.IsAny<Func<BasicDeliverEventArgs, bool>>()));

		static readonly TimeSpan timeout = TimeSpan.FromMilliseconds(250);
		static int invocations;
	}

	[Subject(typeof(RabbitChannel))]
	public class when_opening_the_channel_to_receive_without_provding_a_callback : using_a_channel
	{
		Because of = () =>
			thrown = Catch.Exception(() => channel.Receive(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();

		static Exception thrown;
	}

	[Subject(typeof(RabbitChannel))]
	public class when_opening_the_channel_for_receive_more_than_once : using_a_channel
	{
		Establish context = () =>
			channel.Receive(delivery => { });

		Because of = () =>
			thrown = Catch.Exception(() => channel.Receive(delivery => { }));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();

		static Exception thrown;
	}

	[Subject(typeof(RabbitChannel))]
	public class when_receiving_a_message : using_a_channel
	{
		Establish context = () =>
			mockAdapter.Setup(x => x.Build(message)).Returns(new Mock<ChannelMessage>().Object);

		Because of = () =>
		{
			channel.Receive(deliveryContext => delivery = deliveryContext);
			Receive(message);
		};

		It should_invoke_the_callback_provided = () =>
			delivery.ShouldNotBeNull();

		It should_begin_a_transaction = () =>
			delivery.CurrentTransaction.ShouldNotBeNull();

		It should_build_the_ChannelMessage = () =>
			mockAdapter.Verify(x => x.Build(message), Times.Once());

		It should_set_the_ChannelMessage_on_the_channel = () =>
			channel.CurrentMessage.ShouldNotBeNull();

		It should_mark_the_transaction_a_finished_after_message_processing_is_complete = () =>
			delivery.CurrentTransaction.Finished.ShouldBeTrue();

		static readonly BasicDeliverEventArgs message = new BasicDeliverEventArgs();
		static IDeliveryContext delivery;
	}

	[Subject(typeof(RabbitChannel))]
	public class when_no_message_is_received_from_the_subscription : using_a_channel
	{
		Establish context = () =>
			channel.Receive(delivery => { });

		Because of = () =>
			Receive(null);

		It should_have_a_fresh_transaction = () =>
			channel.CurrentTransaction.Finished.ShouldBeFalse();

		It should_set_the_CurrentMessage_on_the_channel_to_be_null = () =>
			channel.CurrentMessage.ShouldBeNull();

		It should_not_attempt_to_process_the_null_message = () =>
			mockAdapter.Verify(x => x.Build((BasicDeliverEventArgs)null), Times.Never());
	}

	[Subject(typeof(RabbitChannel))]
	public class when_the_handling_of_a_message_throws_an_exception : using_a_channel
	{
		Establish context = () =>
		{
			mockRealChannel.Setup(x => x.TxRollback());
			mockSubscription.Setup(x => x.RetryMessage(message));

			RequireTransaction(RabbitTransactionType.Full);
			Initialize();
		};

		Because of = () =>
		{
			channel.Receive(delivery => { throw new Exception("Message handling failed"); });
			Receive(message);
		};

		It should_add_the_message_to_the_retry_queue = () =>
			mockSubscription.Verify(x => x.RetryMessage(message));

		It should_finalize_and_dispose_the_outstanding_transaction_for_transactional_channel = () =>
			mockRealChannel.Verify(x => x.TxRollback(), Times.Once());

		static readonly BasicDeliverEventArgs message = new BasicDeliverEventArgs();
	}

	[Subject(typeof(RabbitChannel))]
	public class when_the_handling_of_a_message_throws_a_SerializationException : using_a_channel
	{
		Establish context = () =>
		{
			message = new BasicDeliverEventArgs
			{
				BasicProperties = new Mock<IBasicProperties>().Object,
				Body = new byte[] { 0, 1, 2, 3, 4 }
			};

			mockSubscription.Setup(x => x.AcknowledgeMessage());

			mockRealChannel.Setup(x => x.TxCommit());
			mockRealChannel.Setup(x => x.BasicPublish(address, message.BasicProperties, message.Body));

			var poisonExchange = new Mock<RabbitAddress>();
			poisonExchange.Setup(x => x.Address).Returns(address);
			mockConfiguration.Setup(x => x.PoisonMessageExchange).Returns(poisonExchange.Object);

			mockAdapter.Setup(x => x.Build(message)).Throws(new SerializationException());

			RequireTransaction(RabbitTransactionType.Full);
			Initialize();
		};

		Because of = () =>
		{
			channel.Receive(delivery => { });
			Receive(message);
		};

		It should_dispatch_the_message_to_the_configured_poison_message_exchange = () =>
			mockRealChannel.Verify(x =>
				x.BasicPublish(address, message.BasicProperties, message.Body), Times.Once());

		It should_acknowledge_the_poison_message_from_the_receiving_queue_when_the_channels_uses_ack = () =>
			mockSubscription.Verify(x => x.AcknowledgeMessage(), Times.Once());

		It should_commit_the_transaction_on_fully_transactional_channels = () =>
			mockRealChannel.Verify(x => x.TxCommit(), Times.Once());

		static BasicDeliverEventArgs message;
		static readonly PublicationAddress address =
			new PublicationAddress(string.Empty, string.Empty, string.Empty);
	}

	[Subject(typeof(RabbitChannel))]
	public class when_the_handling_of_a_message_throws_a_ChannelConnectionException : using_a_channel
	{
		Establish context = () =>
			channel.Receive(delivery => { throw new ChannelConnectionException(); });

		Because of = () =>
			thrown = Catch.Exception(() => Receive(new BasicDeliverEventArgs()));

		It should_throw_the_exception = () =>
			thrown.ShouldBeOfType<ChannelConnectionException>();

		It should_mark_the_transaction_a_finished = () =>
			channel.CurrentTransaction.Finished.ShouldBeTrue();

		static Exception thrown;
	}

	[Subject(typeof(RabbitChannel))]
	public class when_sending_a_null_message : using_a_channel
	{
		Because of = () =>
			thrown = Catch.Exception(() => channel.Send(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();

		static Exception thrown;
	}

	[Subject(typeof(RabbitChannel))]
	public class when_sending_a_message : using_a_channel
	{
		Establish context = () =>
		{
			mockEnvelope = new Mock<ChannelEnvelope>();
			mockEnvelope.Setup(x => x.Message).Returns(new Mock<ChannelMessage>().Object);
			mockEnvelope.Setup(x => x.Recipients).Returns(new[]
			{
				ChannelEnvelope.LoopbackAddress,
				ChannelEnvelope.LoopbackAddress
			});

			rabbitMessage = new BasicDeliverEventArgs
			{
				BasicProperties = new Mock<IBasicProperties>().Object,
				Body = new byte[] { 0, 1, 2, 3, 4 }
			};
			mockAdapter.Setup(x => x.Build(mockEnvelope.Object.Message)).Returns(rabbitMessage);
		};

		Because of = () =>
		{
			channel.Send(mockEnvelope.Object);
			channel.CurrentTransaction.Commit();
		};

		It should_build_a_message_specific_the_the_channel_from_the_message_provided = () =>
			mockAdapter.Verify(x => x.Build(mockEnvelope.Object.Message), Times.Once());

		It should_dispatch_the_message_to_the_recipients_specified = () =>
			mockRealChannel.Verify(x => x.BasicPublish(
				Moq.It.IsAny<PublicationAddress>(),
				rabbitMessage.BasicProperties,
				rabbitMessage.Body), Times.Exactly(mockEnvelope.Object.Recipients.Count));

		static Mock<ChannelEnvelope> mockEnvelope;
		static BasicDeliverEventArgs rabbitMessage;
	}

	[Subject(typeof(RabbitChannel))]
	public class when_acknowledging_a_message_against_an_acknowledge_only_channel : using_a_channel
	{
		Establish context = () =>
		{
			RequireTransaction(RabbitTransactionType.Acknowledge);
			mockSubscription.Setup(x => x.AcknowledgeMessage());
			Initialize();

			channel.Receive(delivery => { });
		};

		Because of = () =>
			channel.AcknowledgeMessage();

		It should_ack_against_the_underlying_subscription = () =>
			mockSubscription.Verify(x => x.AcknowledgeMessage(), Times.Once());
	}

	[Subject(typeof(RabbitChannel))]
	public class when_acknowledging_a_message_with_no_transaction : using_a_channel
	{
		Establish context = () =>
			channel.Receive(delivery => { });

		Because of = () =>
			channel.AcknowledgeMessage();

		It should_NOT_ack_against_the_underlying_subscription = () =>
			mockSubscription.Verify(x => x.AcknowledgeMessage(), Times.Never());
	}

	[Subject(typeof(RabbitChannel))]
	public class when_acknowledging_a_message_on_a_full_transaction : using_a_channel
	{
		Establish context = () =>
		{
			RequireTransaction(RabbitTransactionType.Full);
			mockSubscription.Setup(x => x.AcknowledgeMessage());
			Initialize();

			channel.Receive(delivery => { });
		};

		Because of = () =>
			channel.AcknowledgeMessage();

		It should_ack_against_the_underlying_subscription = () =>
			mockSubscription.Verify(x => x.AcknowledgeMessage(), Times.Once());
	}

	[Subject(typeof(RabbitChannel))]
	public class when_acknowledging_without_first_opening_for_receive : using_a_channel
	{
		Because of = () =>
			thrown = Catch.Exception(() => channel.AcknowledgeMessage());

		It should_throw_an_invalid_operation_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();

		It should_NOT_ack_against_the_underlying_subscription = () =>
			mockSubscription.Verify(x => x.AcknowledgeMessage(), Times.Never());

		static Exception thrown;
	}

	[Subject(typeof(RabbitChannel))]
	public class when_committing_a_transaction_against_a_non_transactional_channel : using_a_channel
	{
		Establish context = () =>
			mockRealChannel.Setup(x => x.TxCommit());

		Because of = () =>
			channel.CommitTransaction();

		It should_NOT_invoke_commit_against_the_underlying_channel = () =>
			mockRealChannel.Verify(x => x.TxCommit(), Times.Never());
	}

	[Subject(typeof(RabbitChannel))]
	public class when_committing_a_transaction_against_an_acknowledge_channel : using_a_channel
	{
		Establish context = () =>
		{
			RequireTransaction(RabbitTransactionType.Acknowledge);
			Initialize();

			mockRealChannel.Setup(x => x.TxCommit());
		};

		Because of = () =>
			channel.CommitTransaction();

		It should_NOT_invoke_commit_against_the_underlying_channel = () =>
			mockRealChannel.Verify(x => x.TxCommit(), Times.Never());
	}

	[Subject(typeof(RabbitChannel))]
	public class when_committing_a_transaction_against_a_transactional_channel : using_a_channel
	{
		Establish context = () =>
		{
			RequireTransaction(RabbitTransactionType.Full);
			Initialize();

			mockRealChannel.Setup(x => x.TxCommit());
		};

		Because of = () =>
			channel.CommitTransaction();

		It should_NOT_invoke_commit_against_the_underlying_channel = () =>
			mockRealChannel.Verify(x => x.TxCommit(), Times.Once());
	}

	[Subject(typeof(RabbitChannel))]
	public class when_rolling_back_a_transaction_against_a_non_transactional_channel : using_a_channel
	{
		Establish context = () =>
			mockRealChannel.Setup(x => x.TxRollback());

		Because of = () =>
			channel.RollbackTransaction();

		It should_NOT_invoke_commit_against_the_underlying_channel = () =>
			mockRealChannel.Verify(x => x.TxRollback(), Times.Never());
	}

	[Subject(typeof(RabbitChannel))]
	public class when_rolling_back_a_transaction_against_an_acknowledge_channel : using_a_channel
	{
		Establish context = () =>
		{
			RequireTransaction(RabbitTransactionType.Acknowledge);
			Initialize();

			mockRealChannel.Setup(x => x.TxRollback());
		};

		Because of = () =>
			channel.RollbackTransaction();

		It should_NOT_invoke_commit_against_the_underlying_channel = () =>
			mockRealChannel.Verify(x => x.TxRollback(), Times.Never());
	}

	[Subject(typeof(RabbitChannel))]
	public class when_rolling_back_a_transaction_against_a_transactional_channel : using_a_channel
	{
		Establish context = () =>
		{
			RequireTransaction(RabbitTransactionType.Full);
			Initialize();

			mockRealChannel.Setup(x => x.TxRollback());
		};

		Because of = () =>
			channel.RollbackTransaction();

		It should_NOT_invoke_commit_against_the_underlying_channel = () =>
			mockRealChannel.Verify(x => x.TxRollback(), Times.Once());
	}

	[Subject(typeof(RabbitChannel))]
	public class when_disposing_a_channel : using_a_channel
	{
		Establish context = () =>
			mockRealChannel.Setup(x => x.Dispose());

		Because of = () =>
			channel.Dispose();

		It should_dispose_the_underlying_channel = () =>
			mockRealChannel.Verify(x => x.Dispose(), Times.Once());
	}

	[Subject(typeof(RabbitChannel))]
	public class when_disposing_a_channel_with_a_subscription : using_a_channel
	{
		Establish context = () =>
		{
			mockSubscription.Setup(x => x.Dispose());
			channel.Receive(delivery => { });
		};

		Because of = () =>
			channel.Dispose();

		It should_dispose_the_subscription = () =>
			mockSubscription.Verify(x => x.Dispose(), Times.Once());
	}

	[Subject(typeof(RabbitChannel))]
	public class when_dispose_is_called_multiple_times : using_a_channel
	{
		Establish context = () =>
			mockRealChannel.Setup(x => x.Dispose());

		Because of = () =>
		{
			channel.Dispose();
			channel.Dispose();
		};

		It should_only_dispose_the_underlying_resources_once = () =>
			mockRealChannel.Verify(x => x.Dispose(), Times.Once());
	}

	[Subject(typeof(RabbitChannel))]
	public class when_attempting_to_send_through_a_disposed_channel : using_a_channel
	{
		Establish context = () =>
			channel.Dispose();

		Because of = () =>
			thrown = Catch.Exception(() => channel.Send(new Mock<ChannelEnvelope>().Object));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();

		static Exception thrown;
	}

	[Subject(typeof(RabbitChannel))]
	public class when_attempting_to_receive_through_a_disposed_channel : using_a_channel
	{
		Establish context = () =>
			channel.Dispose();

		Because of = () =>
			thrown = Catch.Exception(() => channel.Receive(context => { }));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();

		static Exception thrown;
	}

	[Subject(typeof(RabbitChannel))]
	public class when_attempting_to_acknowledge_a_message_against_a_disposed_channel : using_a_channel
	{
		Establish context = () =>
			channel.Dispose();

		Because of = () =>
			thrown = Catch.Exception(() => channel.RollbackTransaction());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();

		static Exception thrown;
	}

	[Subject(typeof(RabbitChannel))]
	public class when_attempting_to_commit_a_transaction_against_a_disposed_channel : using_a_channel
	{
		Establish context = () =>
			channel.Dispose();

		Because of = () =>
			thrown = Catch.Exception(() => channel.CommitTransaction());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();

		static Exception thrown;
	}

	[Subject(typeof(RabbitChannel))]
	public class when_attempting_to_rollback_a_transaction_against_a_disposed_channel : using_a_channel
	{
		Establish context = () =>
			channel.Dispose();

		Because of = () =>
			thrown = Catch.Exception(() => channel.RollbackTransaction());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();

		static Exception thrown;
	}

	[Subject(typeof(RabbitChannel))]
	public class when_initiating_receive_on_a_shutdown_channel : using_a_channel
	{
		Establish context = () =>
			channel.BeginShutdown();

		Because of = () =>
			thrown = Catch.Exception(() => channel.Receive(context => { }));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ChannelShutdownException>();

		static Exception thrown;
	}

	[Subject(typeof(RabbitChannel))]
	public class when_receiving_a_message_on_a_shutdown_channel : using_a_channel
	{
		Establish context = () =>
		{
			channel.Receive(delivery => { });
			channel.BeginShutdown();
		};

		Because of = () =>
			Receive(message);

		It should_not_process_the_message = () =>
			mockAdapter.Verify(x => x.Build(message), Times.Never());

		static readonly BasicDeliverEventArgs message = new BasicDeliverEventArgs();
	}

	[Subject(typeof(RabbitChannel))]
	public class when_attempting_to_send_on_full_duplex_channel_that_is_shutting_down : using_a_channel
	{
		Establish context = () =>
		{
			channel.Receive(delivery => { }); // makes the channel full duplex

			var mockEnvelope = new Mock<ChannelEnvelope>();
			mockEnvelope.Setup(x => x.Recipients).Returns(new Uri[0]);
			mockAdapter.Setup(x => x.Build(mockEnvelope.Object.Message));
			envelope = mockEnvelope.Object;

			channel.BeginShutdown();
		};

		Because of = () =>
			channel.Send(envelope);

		It should_allow_the_dispatch_to_proceed_so_that_the_transaction_can_complete = () =>
			mockAdapter.Verify(x => x.Build(envelope.Message), Times.Once());

		static ChannelEnvelope envelope;
	}

	[Subject(typeof(RabbitChannel))]
	public class when_attempting_to_send_on_a_send_only_channel_that_is_shutting_down : using_a_channel
	{
		Establish context = () =>
			channel.BeginShutdown();

		Because of = () =>
			thrown = Catch.Exception(() => channel.Send(new Mock<ChannelEnvelope>().Object));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ChannelShutdownException>();

		static Exception thrown;
	}

	public abstract class using_a_channel
	{
		Establish context = () =>
		{
			mockRealChannel = new Mock<IModel>();
			mockAdapter = new Mock<RabbitMessageAdapter>();
			mockConfiguration = new Mock<RabbitChannelGroupConfiguration>();
			mockSubscription = new Mock<RabbitSubscription>();

			var timeout = TimeSpan.FromMilliseconds(100);
			mockConfiguration.Setup(x => x.ReceiveTimeout).Returns(timeout);
			mockSubscription
				.Setup(x => x.BeginReceive(timeout, Moq.It.IsAny<Func<BasicDeliverEventArgs, bool>>()))
				.Callback<TimeSpan, Func<BasicDeliverEventArgs, bool>>((first, second) => { dispatch = second; });

			RequireTransaction(RabbitTransactionType.None);
			Initialize();
		};

		protected static void RequireTransaction(RabbitTransactionType transactionType)
		{
			mockConfiguration.Setup(x => x.TransactionType).Returns(transactionType);
		}
		protected static void Initialize()
		{
			channel = new RabbitChannel(
				mockRealChannel.Object, mockAdapter.Object, mockConfiguration.Object, () => mockSubscription.Object);
		}
		protected static void Receive(BasicDeliverEventArgs message)
		{
			dispatch(message);
		}

		protected const string DefaultChannelGroup = "some group name";
		protected static Mock<IModel> mockRealChannel;
		protected static Mock<RabbitMessageAdapter> mockAdapter;
		protected static Mock<RabbitChannelGroupConfiguration> mockConfiguration;
		protected static Mock<RabbitSubscription> mockSubscription;
		protected static RabbitChannel channel;
		static Func<BasicDeliverEventArgs, bool> dispatch;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169