#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.RabbitChannel
{
	using System;
	using Machine.Specifications;
	using Moq;
	using RabbitMQ.Client;
	using It = Machine.Specifications.It;

	[Subject(typeof(RabbitTransaction))]
	public class when_an_action_is_registered_with_the_transaction : using_a_transaction
	{
		Because of = () =>
			transaction.Register(callback);

		It should_not_invoke_the_callback = () =>
			invocations.ShouldEqual(0);
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

		static Exception thrown;
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

		static Exception thrown;
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_an_action_is_registered_with_the_committed_transaction : using_a_transaction
	{
		Establish context = () =>
			transaction.Commit();

		Because of = () =>
			thrown = Catch.Exception(() => transaction.Register(callback));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();

		static Exception thrown;
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_a_null_action_is_registered_with_the_transaction : using_a_transaction
	{
		Because of = () =>
			thrown = Catch.Exception(() => transaction.Register(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();

		static Exception thrown;
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
			mockChannel.Setup(x => x.TxCommit());
			mockSubscription.Setup(x => x.AcknowledgeReceipt());
			InitializeTransaction();
		};

		Because of = () =>
			transaction.Commit();

		It should_call_ack_on_the_underlying_subscription = () =>
			mockSubscription.Verify(x => x.AcknowledgeReceipt(), Times.Once());

		It should_NOT_call_commit_on_the_underlying_channel = () =>
			mockChannel.Verify(x => x.TxCommit(), Times.Never());
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_committing_full_transaction : using_a_transaction
	{
		Establish context = () =>
		{
			transactionType = RabbitTransactionType.Full;
			mockChannel.Setup(x => x.TxCommit());
			mockSubscription.Setup(x => x.AcknowledgeReceipt());
			InitializeTransaction();
		};

		Because of = () =>
			transaction.Commit();

		It should_call_ack_on_the_underlying_subscription = () =>
			mockSubscription.Verify(x => x.AcknowledgeReceipt(), Times.Once());

		It should_commit_the_transaction_on_the_underlying_channel = () =>
			mockChannel.Verify(x => x.TxCommit(), Times.Once());
	}
	
	[Subject(typeof(RabbitTransaction))]
	public class when_commiting_a_disposed_transaction : using_a_transaction
	{
		Establish context = () =>
			transaction.Dispose();

		Because of = () =>
			thrown = Catch.Exception(() => transaction.Commit());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();

		static Exception thrown;
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_attempting_to_commit_a_rolled_back_transaction : using_a_transaction
	{
		Establish context = () =>
			transaction.Rollback();

		Because of = () =>
			thrown = Catch.Exception(() => transaction.Commit());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();

		static Exception thrown;
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_rolling_back_a_transaction : using_a_transaction
	{
		Because of = () =>
			transaction.Rollback();

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
			mockChannel.Setup(x => x.TxRollback());
			InitializeTransaction();
		};

		Because of = () =>
			transaction.Rollback();

		It should_rollback_the_transaction_against_the_underlying_model = () =>
			mockChannel.Verify(x => x.TxRollback(), Times.Once());
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_rolling_back_a_transaction_more_than_once : using_a_transaction
	{
		Establish context = () =>
		{
			transactionType = RabbitTransactionType.Full;
			mockChannel.Setup(x => x.TxRollback());
			InitializeTransaction();

			transaction.Rollback();
		};

		Because of = () =>
			transaction.Rollback();

		It should_not_do_anything = () =>
			mockChannel.Verify(x => x.TxRollback(), Times.Once());
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_rolling_back_a_committed_transaction : using_a_transaction
	{
		Establish context = () =>
			transaction.Commit();

		Because of = () =>
			thrown = Catch.Exception(() => transaction.Rollback());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();

		static Exception thrown;
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

		static Exception thrown;
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
			mockChannel.Setup(x => x.TxRollback());
			InitializeTransaction();
		};

		Because of = () =>
			transaction.Dispose();

		It should_rollback_the_transaction_against_the_underlying_model = () =>
			mockChannel.Verify(x => x.TxRollback(), Times.Once());
	}

	[Subject(typeof(RabbitTransaction))]
	public class when_disposing_a_transaction_more_than_once : using_a_transaction
	{
		Establish context = () =>
		{
			transactionType = RabbitTransactionType.Full;
			mockChannel.Setup(x => x.TxRollback());
			InitializeTransaction();

			transaction.Dispose();
		};

		Because of = () =>
			transaction.Dispose();

		It should_rollback_the_transaction_against_the_underlying_model_exactly_once = () =>
			mockChannel.Verify(x => x.TxRollback(), Times.Once());
	}

	public abstract class using_a_transaction
	{
		protected static RabbitTransactionType transactionType = RabbitTransactionType.None;
		protected static RabbitTransaction transaction;
		protected static Mock<IModel> mockChannel;
		protected static Mock<RabbitSubscription> mockSubscription;
		protected static int invocations;
		protected static Action callback = () => { invocations++; };

		Establish context = () =>
		{
			mockChannel = new Mock<IModel>();
			mockSubscription = new Mock<RabbitSubscription>();

			InitializeTransaction();
		};

		protected static void InitializeTransaction()
		{
			transaction = new RabbitTransaction(mockChannel.Object, mockSubscription.Object, transactionType);
		}

		Cleanup after = () =>
		{
			invocations = 0;
			transaction = null;
			mockSubscription = null;
			mockChannel = null;
			transactionType = RabbitTransactionType.None;
		};
	}

	public class Subscription : RabbitMQ.Client.MessagePatterns.Subscription
	{
		public Subscription() : base(null, null) { }
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169