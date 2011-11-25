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
			channel.BeginReceive(delivery => { });

		It should_call_the_subscription_factory = () =>
			invocations.ShouldEqual(1);

		It should_open_the_subscription_to_receive = () =>
			mockSubscription.Verify(x =>
				x.BeginReceive(timeout, Moq.It.IsAny<Action<BasicDeliverEventArgs>>()));

		static readonly TimeSpan timeout = TimeSpan.FromMilliseconds(250);
		static int invocations;
	}

	[Subject(typeof(RabbitChannel))]
	public class when_opening_the_channel_to_receive_without_provding_a_callback : using_a_channel
	{
		Because of = () =>
			thrown = Catch.Exception(() => channel.BeginReceive(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();

		static Exception thrown;
	}

	[Subject(typeof(RabbitChannel))]
	public class when_opening_the_channel_for_receive_more_than_once : using_a_channel
	{
		Establish context = () =>
			channel.BeginReceive(delivery => { });

		Because of = () =>
			thrown = Catch.Exception(() => channel.BeginReceive(delivery => { }));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();

		static Exception thrown;
	}

	[Subject(typeof(RabbitChannel))]
	public class when_receiving_a_message : using_a_channel
	{
		Establish context = () =>
		{
			mockAdapter.Setup(x => x.Build(message)).Returns(new Mock<ChannelMessage>().Object);
			mockSubscription
				.Setup(x => x.BeginReceive(Moq.It.IsAny<TimeSpan>(), Moq.It.IsAny<Action<BasicDeliverEventArgs>>()))
				.Callback<TimeSpan, Action<BasicDeliverEventArgs>>((first, second) => { dispatch = second; });
		};

		Because of = () =>
		{
			channel.BeginReceive(deliveryContext => delivery = deliveryContext);
			dispatch(message);
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
		static Action<BasicDeliverEventArgs> dispatch;
		static IDeliveryContext delivery;
	}

	[Subject(typeof(RabbitChannel))]
	public class when_the_handling_of_a_message_throws_an_exception : using_a_channel
	{
		Establish context = () =>
		{
			RequireTransaction(RabbitTransactionType.Full);
			mockRealChannel.Setup(x => x.TxRollback());
			mockSubscription.Setup(x => x.RetryMessage(message));
			mockSubscription
				.Setup(x => x.BeginReceive(Moq.It.IsAny<TimeSpan>(), Moq.It.IsAny<Action<BasicDeliverEventArgs>>()))
				.Callback<TimeSpan, Action<BasicDeliverEventArgs>>((first, second) => { dispatch = second; });

			Initialize();
		};

		Because of = () =>
		{
			channel.BeginReceive(delivery => { throw new Exception("Message handling failed"); });
			dispatch(message);
		};

		It should_add_the_message_to_the_retry_queue = () =>
			mockSubscription.Verify(x => x.RetryMessage(message));

		It should_finalize_and_dispose_the_outstanding_transaction_for_transactional_channel = () =>
			mockRealChannel.Verify(x => x.TxRollback(), Times.Once());

		static Action<BasicDeliverEventArgs> dispatch;
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

			mockSubscription
				.Setup(x => x.BeginReceive(Moq.It.IsAny<TimeSpan>(), Moq.It.IsAny<Action<BasicDeliverEventArgs>>()))
				.Callback<TimeSpan, Action<BasicDeliverEventArgs>>((first, second) => { dispatch = second; });
			mockSubscription.Setup(x => x.AcknowledgeMessage());

			mockRealChannel.Setup(x => x.TxCommit());
			mockRealChannel
				.Setup(x => x.BasicPublish(address, message.BasicProperties, message.Body));

			var poisonExchange = new Mock<RabbitAddress>();
			poisonExchange.Setup(x => x.Address).Returns(address);
			mockConfiguration.Setup(x => x.PoisonMessageExchange).Returns(poisonExchange.Object);

			mockAdapter.Setup(x => x.Build(message)).Throws(new SerializationException());

			RequireTransaction(RabbitTransactionType.Full);
			Initialize();
		};

		Because of = () =>
		{
			channel.BeginReceive(delivery => { });
			dispatch(message);
		};

		It should_dispatch_the_message_to_the_configured_poison_message_exchange = () =>
			mockRealChannel.Verify(x =>
				x.BasicPublish(address, message.BasicProperties, message.Body), Times.Once());

		It should_acknowledge_the_poison_message_from_the_receiving_queue_when_the_channels_uses_ack = () =>
			mockSubscription.Verify(x => x.AcknowledgeMessage(), Times.Once());

		It should_commit_the_transaction_on_fully_transactional_channels = () =>
			mockRealChannel.Verify(x => x.TxCommit(), Times.Once());

		static BasicDeliverEventArgs message;
		static Action<BasicDeliverEventArgs> dispatch;
		static readonly PublicationAddress address =
			new PublicationAddress(string.Empty, string.Empty, string.Empty);
	}

	[Subject(typeof(RabbitChannel))]
	public class when_the_handling_of_a_message_throws_a_ChannelConnectionException : using_a_channel
	{
		Establish context = () => mockSubscription
			.Setup(x => x.BeginReceive(Moq.It.IsAny<TimeSpan>(), Moq.It.IsAny<Action<BasicDeliverEventArgs>>()))
			.Callback<TimeSpan, Action<BasicDeliverEventArgs>>((first, second) => { dispatch = second; });

		Because of = () =>
		{
			channel.BeginReceive(delivery => { throw new ChannelConnectionException(); });
			thrown = Catch.Exception(() => dispatch(new BasicDeliverEventArgs()));
		};

		It should_throw_the_exception = () =>
			thrown.ShouldBeOfType<ChannelConnectionException>();

		It should_mark_the_transaction_a_finished = () =>
			channel.CurrentTransaction.Finished.ShouldBeTrue();

		static Action<BasicDeliverEventArgs> dispatch;
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

			channel.BeginReceive(delivery => { });
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
			channel.BeginReceive(delivery => { });

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

			channel.BeginReceive(delivery => { });
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
			channel.BeginReceive(delivery => { });
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
			thrown = Catch.Exception(() => channel.BeginReceive(context => { }));

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

	public abstract class using_a_channel
	{
		Establish context = () =>
		{
			mockRealChannel = new Mock<IModel>();
			mockAdapter = new Mock<RabbitMessageAdapter>();
			mockConfiguration = new Mock<RabbitChannelGroupConfiguration>();
			mockSubscription = new Mock<RabbitSubscription>();

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

		protected const string DefaultChannelGroup = "some group name";
		protected static Mock<IModel> mockRealChannel;
		protected static Mock<RabbitMessageAdapter> mockAdapter;
		protected static Mock<RabbitChannelGroupConfiguration> mockConfiguration;
		protected static Mock<RabbitSubscription> mockSubscription;
		protected static RabbitChannel channel;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169