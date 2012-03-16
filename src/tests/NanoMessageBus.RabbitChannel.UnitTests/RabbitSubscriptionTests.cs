#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.Channels
{
	using System;
	using Machine.Specifications;
	using Moq;
	using RabbitMQ.Client.Events;
	using RabbitMQ.Client.Exceptions;
	using It = Machine.Specifications.It;

	[Subject(typeof(RabbitSubscription))]
	public class when_a_negative_receive_timeout_is_specified : using_a_subscription
	{
		Because of = () =>
			thrown = Catch.Exception(() =>
				subscription.Receive(ZeroTimeout, DisposeCallback));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentException>();

		static readonly TimeSpan ZeroTimeout = TimeSpan.FromMilliseconds(-1);
	}

	[Subject(typeof(RabbitSubscription))]
	public class when_attempting_to_receive_without_providing_a_callback : using_a_subscription
	{
		Because of = () =>
			exception = Catch.Exception(() =>
				subscription.Receive(DefaultTimeout, null));

		It should_throw_an_exception = () =>
			exception.ShouldBeOfType<ArgumentNullException>();

		static Exception exception;
	}

	[Subject(typeof(RabbitSubscription))]
	public class when_attempting_to_receive_from_a_disposed_subscription : using_a_subscription
	{
		Establish context = () =>
			subscription.Dispose();

		Because of = () =>
			thrown = Catch.Exception(() =>
				subscription.Receive(DefaultTimeout, DisposeCallback));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(RabbitSubscription))]
	public class when_the_callback_returns_false : using_a_subscription
	{
		Establish context = () =>
			mockRealSubscription.Setup(x => x.BeginReceive(DefaultTimeout)).Returns((BasicDeliverEventArgs)null);

		Because of = () =>
			subscription.Receive(DefaultTimeout, delivery => ++invocations > 1);

		It should_invoke_the_callback_and_then_exit_the_receive_loop = () =>
			invocations.ShouldEqual(1);
	}

	[Subject(typeof(RabbitSubscription))]
	public class when_a_subscription_is_disposed_during_receive : using_a_subscription
	{
		Because of = () =>
			subscription.Receive(DefaultTimeout, DisposeCallback);

		It should_exit_the_receive_loop = () =>
			true.ShouldBeTrue(); // the fact that we got here means the loop exited
	}

	[Subject(typeof(RabbitSubscription))]
	public class when_receiving_a_message_from_a_subscription : using_a_subscription
	{
		Establish context = () => mockRealSubscription
			.Setup(x => x.BeginReceive(DefaultTimeout))
			.Returns(new Mock<BasicDeliverEventArgs>().Object);

		Because of = () => subscription.Receive(DefaultTimeout, msg =>
		{
			message = msg;
			return false; // finished receiving
		});

		It should_invoke_the_callback_with_the_received_message = () =>
			message.ShouldNotBeNull();

		static BasicDeliverEventArgs message;
	}

	[Subject(typeof(RabbitSubscription))]
	public class when_no_message_is_received_from_the_subscription_after_the_timeout_indicated : using_a_subscription
	{
		Establish context = () => mockRealSubscription
			.Setup(x => x.BeginReceive(DefaultTimeout))
			.Returns((BasicDeliverEventArgs)null);

		Because of = () => subscription.Receive(DefaultTimeout, msg =>
		{
			invocations++;
			return false; // finished receiving
		});

		It should_invoke_the_callback = () =>
			invocations.ShouldEqual(1);
	}

	[Subject(typeof(RabbitSubscription))]
	public class when_receiving_messages_from_a_subscription : using_a_subscription
	{
		Establish context = () => mockRealSubscription
			.Setup(x => x.BeginReceive(DefaultTimeout))
			.Returns(new Mock<BasicDeliverEventArgs>().Object);

		Because of = () => subscription.Receive(DefaultTimeout, msg =>
			MaxInvocations != ++invocations);

		It should_loop_until_the_subscription_is_disposed = () =>
			invocations.ShouldEqual(MaxInvocations);

		const int MaxInvocations = 3;
	}

	[Subject(typeof(RabbitSubscription))]
	public class when_receiving_a_message_throws_an_OperationInterruptedException : using_a_subscription
	{
		Establish context = () => mockRealSubscription
			.Setup(x => x.BeginReceive(DefaultTimeout))
			.Throws(new OperationInterruptedException(null));

		Because of = () =>
			thrown = Catch.Exception(() => subscription.Receive(DefaultTimeout, DisposeCallback));

		It should_throw_a_ChannelConnectionException = () =>
			thrown.ShouldBeOfType<ChannelConnectionException>();
	}

	[Subject(typeof(RabbitSubscription))]
	public class when_acknowledging_the_receipt_of_a_message : using_a_subscription
	{
		Establish context = () =>
			mockRealSubscription.Setup(x => x.AcknowledgeMessages());

		Because of = () =>
			subscription.AcknowledgeMessages();

		It should_acknowledge_the_receipt_to_the_underlying_subscription = () =>
			mockRealSubscription.Verify(x => x.AcknowledgeMessages(), Times.Once());
	}

	[Subject(typeof(RabbitSubscription))]
	public class when_acknowledging_the_receipt_of_a_message_on_a_disposed_subscription : using_a_subscription
	{
		Establish context = () =>
			subscription.Dispose();

		Because of = () =>
			thrown = Catch.Exception(() => subscription.AcknowledgeMessages());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(RabbitSubscription))]
	public class when_disposing_a_subscription : using_a_subscription
	{
		Establish context = () =>
			mockRealSubscription.Setup(x => x.Dispose());

		Because of = () =>
			subscription.Dispose();

		It should_dispose_the_subscription = () => 
			mockRealSubscription.Verify(x => x.Dispose(), Times.Once());
	}

	[Subject(typeof(RabbitSubscription))]
	public class when_disposing_a_subscription_which_throws_an_exception : using_a_subscription
	{
		Establish context = () =>
			mockRealSubscription.Setup(x => x.Dispose()).Throws(new Exception());

		Because of = () =>
			subscription.Dispose();

		It should_suppress_the_exception = () =>
			thrown.ShouldEqual(null);
	}

	public abstract class using_a_subscription
	{
		Establish context = () =>
		{
			invocations = 0;
			thrown = null;
			mockRealSubscription = new Mock<Subscription>();
			subscription = new RabbitSubscription(mockRealSubscription.Object);
		};

		protected static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(1);
		protected static Mock<Subscription> mockRealSubscription;
		protected static RabbitSubscription subscription;
		protected static int invocations;
		protected static readonly Func<BasicDeliverEventArgs, bool> DisposeCallback = context =>
		{
			subscription.Dispose();
			return true;
		};

		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169