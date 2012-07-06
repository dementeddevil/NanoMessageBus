#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.Channels
{
	using System;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject(typeof(RabbitTransaction))]
	public class when_an_action_is_registered_with_a_non_transaction : using_a_transaction
	{
		Because of = () =>
			transaction.Register(callback);

		It should_invoke_the_callback_immediately = () =>
			invocations.ShouldEqual(1);
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_an_action_is_registered_with_an_acknowledge_transaction : using_a_transaction
	{
		Establish setup = () =>
		{
			transactionType = RabbitTransactionType.Acknowledge;
			Initialize();
		};

		Because of = () =>
			transaction.Register(callback);

		It should_not_invoke_the_callback = () =>
			invocations.ShouldEqual(0);
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_an_action_is_registered_with_a_full_transaction : using_a_transaction
	{
		Establish setup = () =>
		{
			transactionType = RabbitTransactionType.Full;
			Initialize();
		};

		Because of = () =>
			transaction.Register(callback);

		It should_not_invoke_the_callback = () =>
			invocations.ShouldEqual(0);
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_committing_a_transaction_where_no_actions_have_been_registered : using_a_transaction
	{
		Establish context = () =>
		{
			transactionType = RabbitTransactionType.Full;
			Initialize();
		};

		Because of = () =>
			transaction.Commit();

		It should_NOT_acknowledge_message_delivery_to_the_underlying_channel = () =>
			mockChannel.Verify(x => x.AcknowledgeMessage(), Times.Never());

		It should_NOT_commit_against_the_underlying_channel = () =>
			mockChannel.Verify(x => x.CommitTransaction(), Times.Never());
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_an_action_is_registered_with_the_disposed_transaction : using_a_transaction
	{
		Establish context = () =>
			transaction.Dispose();

		Because of = () =>
			thrown = Catch.Exception(() => transaction.Register(callback));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_an_action_is_registered_with_the_rolled_back_transaction : using_a_transaction
	{
		Establish context = () =>
			transaction.Rollback();

		Because of = () =>
			thrown = Catch.Exception(() => transaction.Register(callback));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_an_action_is_registered_as_the_transaction_is_being_committed : using_a_transaction
	{
		Establish context = () =>
		{
			transactionType = RabbitTransactionType.Full;
			Initialize();

			transaction.Register(() =>
			{
				finished = transaction.Finished;
				transaction.Register(callback);
			});
		};

		Because of = () =>
			transaction.Commit();

		It should_NOT_consider_the_transaction_as_committed = () =>
			finished.ShouldBeFalse();

		It should_invoke_the_callback_registration = () =>
			invocations.ShouldEqual(1);

		static bool finished;
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_all_callbacks_have_completed_and_the_channel_is_notified_to_commit : using_a_transaction
	{
		Establish context = () =>
		{
			transactionType = RabbitTransactionType.Full;
			Initialize();

			transaction.Register(callback);
			mockChannel.Setup(x => x.CommitTransaction()).Callback(() => finished = transaction.Finished);
		};

		Because of = () =>
			transaction.Commit();

		It should_consider_the_transaction_as_committed = () =>
			finished.ShouldBeTrue();

		static bool finished;
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_an_action_is_registered_with_the_committed_transaction : using_a_transaction
	{
		Establish context = () =>
		{
			transaction.Register(callback);
			transaction.Commit();
		};

		Because of = () =>
			thrown = Catch.Exception(() => transaction.Register(callback));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_a_null_action_is_registered_with_the_transaction : using_a_transaction
	{
		Because of = () =>
			thrown = Catch.Exception(() => transaction.Register(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_committing_a_transaction : using_a_transaction
	{
		Establish context = () =>
			transaction.Register(callback);

		Because of = () =>
			transaction.Commit();

		It should_invoke_all_registered_callbacks = () =>
			invocations.ShouldEqual(1);

		It should_mark_the_transaction_a_finished = () =>
			transaction.Finished.ShouldBeTrue();
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_committing_a_transaction_more_than_once : using_a_transaction
	{
		Establish context = () =>
		{
			transaction.Register(callback);
			transaction.Commit();
		};

		Because of = () =>
			transaction.Commit();

		It should_only_invoke_the_registered_callbacks_once = () =>
			invocations.ShouldEqual(1);
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_committing_an_acknowledge_only_transaction : using_a_transaction
	{
		Establish context = () =>
		{
			transactionType = RabbitTransactionType.Acknowledge;
			mockChannel.Setup(x => x.CommitTransaction());
			mockChannel.Setup(x => x.AcknowledgeMessage());
			Initialize();

			transaction.Register(callback);
		};

		Because of = () =>
			transaction.Commit();

		It should_call_ack_on_the_underlying_subscription = () =>
			mockChannel.Verify(x => x.AcknowledgeMessage(), Times.Once());

		It should_NOT_call_commit_on_the_underlying_channel = () =>
			mockChannel.Verify(x => x.CommitTransaction(), Times.Never());
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_committing_full_transaction : using_a_transaction
	{
		Establish context = () =>
		{
			transactionType = RabbitTransactionType.Full;
			mockChannel.Setup(x => x.CommitTransaction());
			mockChannel.Setup(x => x.AcknowledgeMessage());

			Initialize();

			transaction.Register(callback);
		};

		Because of = () =>
			transaction.Commit();

		It should_call_ack_on_the_underlying_subscription = () =>
			mockChannel.Verify(x => x.AcknowledgeMessage(), Times.Once());

		It should_commit_the_transaction_on_the_underlying_channel = () =>
			mockChannel.Verify(x => x.CommitTransaction(), Times.Once());
	}
	
	[Subject(typeof(RabbitTransaction))]
	public class when_commiting_a_disposed_transaction : using_a_transaction
	{
		Establish context = () =>
		{
			transaction.Register(callback);
			transaction.Dispose();
		};

		Because of = () =>
			thrown = Catch.Exception(() => transaction.Commit());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_attempting_to_commit_a_rolled_back_transaction : using_a_transaction
	{
		Establish context = () =>
		{
			transaction.Register(callback);
			transaction.Rollback();
		};

		Because of = () =>
			thrown = Catch.Exception(() => transaction.Commit());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_rolling_back_a_transaction : using_a_transaction
	{
		Because of = () =>
		{
			transactionType = RabbitTransactionType.Acknowledge;

			Initialize();
			transaction.Register(callback);
			transaction.Rollback();
		};

		It should_mark_the_transaction_as_finished = () =>
			transaction.Finished.ShouldBeTrue();

		It should_not_invoke_the_registered_callbacks = () =>
			invocations.ShouldEqual(0);
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_rolling_back_a_full_transaction : using_a_transaction
	{
		Establish context = () =>
		{
			transactionType = RabbitTransactionType.Full;
			mockChannel.Setup(x => x.RollbackTransaction());
			Initialize();

			transaction.Register(callback);
		};

		Because of = () =>
			transaction.Rollback();

		It should_rollback_the_transaction_against_the_underlying_model = () =>
			mockChannel.Verify(x => x.RollbackTransaction(), Times.Once());
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_rolling_back_a_transaction_more_than_once : using_a_transaction
	{
		Establish context = () =>
		{
			transactionType = RabbitTransactionType.Full;
			mockChannel.Setup(x => x.RollbackTransaction());
			Initialize();

			transaction.Register(callback);
			transaction.Rollback();
		};

		Because of = () =>
			transaction.Rollback();

		It should_not_do_anything = () =>
			mockChannel.Verify(x => x.RollbackTransaction(), Times.Once());
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_rolling_back_a_committed_transaction : using_a_transaction
	{
		Establish context = () =>
		{
			transaction.Register(callback);
			transaction.Commit();
		};

		Because of = () =>
			thrown = Catch.Exception(() => transaction.Rollback());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_rolling_back_a_disposed_transaction : using_a_transaction
	{
		Establish context = () =>
			transaction.Dispose();

		Because of = () =>
			thrown = Catch.Exception(() => transaction.Rollback());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_disposing_a_transaction : using_a_transaction
	{
		Because of = () =>
			transaction.Dispose();

		It should_mark_the_transaction_as_finished = () =>
			transaction.Finished.ShouldBeTrue();

		It should_not_invoke_the_registered_callbacks = () =>
			invocations.ShouldEqual(0);
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_disposing_a_full_transaction : using_a_transaction
	{
		Establish context = () =>
		{
			transactionType = RabbitTransactionType.Full;
			mockChannel.Setup(x => x.RollbackTransaction());
			Initialize();

			transaction.Register(callback);
		};

		Because of = () =>
			transaction.Dispose();

		It should_rollback_the_transaction_against_the_underlying_model = () =>
			mockChannel.Verify(x => x.RollbackTransaction(), Times.Once());
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_disposing_a_committed_transaction : using_a_transaction
	{
		Establish context = () =>
		{
			transactionType = RabbitTransactionType.Full;
			mockChannel.Setup(x => x.RollbackTransaction());
			Initialize();

			transaction.Register(callback);
			transaction.Commit();
		};

		Because of = () =>
			transaction.Dispose();

		It should_rollback_the_transaction_against_the_underlying_model = () =>
			mockChannel.Verify(x => x.RollbackTransaction(), Times.Never());
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_disposing_a_rolled_back_transaction : using_a_transaction
	{
		Establish context = () =>
		{
			transactionType = RabbitTransactionType.Full;
			mockChannel.Setup(x => x.RollbackTransaction());
			Initialize();

			transaction.Register(callback);
			transaction.Rollback();
		};

		Because of = () =>
			transaction.Dispose();

		It should_rollback_the_transaction_against_the_underlying_model = () =>
			mockChannel.Verify(x => x.RollbackTransaction(), Times.Once());
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_disposing_a_transaction_more_than_once : using_a_transaction
	{
		Establish context = () =>
		{
			transactionType = RabbitTransactionType.Full;
			mockChannel.Setup(x => x.RollbackTransaction());
			Initialize();

			transaction.Register(callback);
			transaction.Dispose();
		};

		Because of = () =>
			transaction.Dispose();

		It should_rollback_the_transaction_against_the_underlying_model_exactly_once = () =>
			mockChannel.Verify(x => x.RollbackTransaction(), Times.Once());
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_dispose_raises_a_ChannelConnectionException : using_a_transaction
	{
		Establish context = () =>
		{
			transactionType = RabbitTransactionType.Full;
			mockChannel.Setup(x => x.RollbackTransaction()).Throws(new ChannelConnectionException());

			Initialize();
		};

		Because of = () =>
			thrown = Catch.Exception(() => transaction.Dispose());

		It should_suppress_the_transaction = () =>
			thrown.ShouldBeNull();
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_dispose_raises_an_ObjectDisposedException : using_a_transaction
	{
		Establish context = () =>
		{
			transactionType = RabbitTransactionType.Full;
			mockChannel.Setup(x => x.RollbackTransaction()).Throws(new ObjectDisposedException(typeof(RabbitChannel).Name));

			Initialize();
		};

		Because of = () =>
			thrown = Catch.Exception(() => transaction.Dispose());

		It should_suppress_the_transaction = () =>
			thrown.ShouldBeNull();
	}

	public abstract class using_a_transaction
	{
		Establish context = () =>
		{
			invocations = 0;
			transactionType = RabbitTransactionType.None;
			mockChannel = new Mock<RabbitChannel>();
			Initialize();
		};

		protected static void Initialize()
		{
			transaction = new RabbitTransaction(mockChannel.Object, transactionType);
		}

		protected static RabbitTransactionType transactionType = RabbitTransactionType.None;
		protected static RabbitTransaction transaction;
		protected static Mock<RabbitChannel> mockChannel;
		protected static int invocations;
		protected static Action callback = () => { invocations++; };
		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169