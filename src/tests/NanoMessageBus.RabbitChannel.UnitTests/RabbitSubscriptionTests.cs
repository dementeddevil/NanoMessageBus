﻿#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.RabbitChannel
{
	using System;
	using Machine.Specifications;
	using Moq;
	using RabbitMQ.Client.Events;
	using It = Machine.Specifications.It;

	[Subject(typeof(RabbitSubscription))]
	public class when_a_negative_receive_timeout_is_specified : using_a_subscription
	{
		Because of = () =>
			thrown = Catch.Exception(() =>
				subscription.BeginReceive(ZeroTimeout, msg => true));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentException>();

		static readonly TimeSpan ZeroTimeout = TimeSpan.FromMilliseconds(-1);
		static Exception thrown;
	}

	[Subject(typeof(RabbitSubscription))]
	public class when_attempting_to_receive_without_providing_a_callback : using_a_subscription
	{
		Because of = () =>
			exception = Catch.Exception(() =>
				subscription.BeginReceive(DefaultTimeout, null));

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
				subscription.BeginReceive(DefaultTimeout, msg => true));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();

		static Exception thrown;
	}

	[Subject(typeof(RabbitSubscription))]
	public class when_the_callback_returns_false : using_a_subscription
	{
		Establish context = () =>
			mockRealSubscription.Setup(x => x.BeginReceive(DefaultTimeout)).Returns((BasicDeliverEventArgs)null);

		Because of = () =>
			subscription.BeginReceive(DefaultTimeout, delivery => ++invocations > 1);

		It should_invoke_the_callback_and_then_exit_the_receive_loop = () =>
			invocations.ShouldEqual(1);

		static int invocations;
	}

	[Subject(typeof(RabbitSubscription))]
	public class when_receiving_a_message_from_a_subscription : using_a_subscription
	{
		Establish context = () => mockRealSubscription
			.Setup(x => x.BeginReceive(DefaultTimeout))
			.Returns(new Mock<BasicDeliverEventArgs>().Object);

		Because of = () => subscription.BeginReceive(DefaultTimeout, msg =>
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

		Because of = () => subscription.BeginReceive(DefaultTimeout, msg =>
		{
			invocations++;
			return false; // finished receiving
		});

		It should_invoke_the_callback = () =>
			invocations.ShouldEqual(1);

		static int invocations;
	}

	[Subject(typeof(RabbitSubscription))]
	public class when_receiving_messages_from_a_subscription : using_a_subscription
	{
		Establish context = () => mockRealSubscription
			.Setup(x => x.BeginReceive(DefaultTimeout))
			.Returns(new Mock<BasicDeliverEventArgs>().Object);

		Because of = () => subscription.BeginReceive(DefaultTimeout, msg =>
			MaxInvocations != ++invocations);

		It should_loop_until_the_subscription_is_disposed = () =>
			invocations.ShouldEqual(MaxInvocations);

		const int MaxInvocations = 3;
		static int invocations;
	}

	[Subject(typeof(RabbitSubscription))]
	public class when_acknowledging_the_receipt_of_a_message : using_a_subscription
	{
		Establish context = () =>
			mockRealSubscription.Setup(x => x.AcknowledgeMessage());

		Because of = () =>
			subscription.AcknowledgeMessage();

		It should_acknowledge_the_receipt_to_the_underlying_subscription = () =>
			mockRealSubscription.Verify(x => x.AcknowledgeMessage(), Times.Once());
	}

	[Subject(typeof(RabbitSubscription))]
	public class when_acknowledging_the_receipt_of_a_message_on_a_disposed_subscription : using_a_subscription
	{
		Establish context = () =>
			subscription.Dispose();

		Because of = () =>
			thrown = Catch.Exception(() => subscription.AcknowledgeMessage());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();

		static Exception thrown;
	}

	[Subject(typeof(RabbitSubscription))]
	public class when_retrying_message : using_a_subscription
	{
		Establish context = () =>
			mockRealSubscription.Setup(x => x.RetryMessage(Moq.It.IsAny<BasicDeliverEventArgs>()));

		Because of = () =>
			subscription.RetryMessage(new Mock<BasicDeliverEventArgs>().Object);

		It should_pass_the_retry_attempt_to_the_underlying_subscription = () =>
			mockRealSubscription.Verify(x => x.RetryMessage(Moq.It.IsAny<BasicDeliverEventArgs>()), Times.Once());
	}

	[Subject(typeof(RabbitSubscription))]
	public class when_retrying_a_message_with_a_disposed_subscription : using_a_subscription
	{
		Establish context = () =>
			subscription.Dispose();

		Because of = () =>
			thrown = Catch.Exception(() => subscription.RetryMessage(new Mock<BasicDeliverEventArgs>().Object));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();

		static Exception thrown;
	}

	[Subject(typeof(RabbitSubscription))]
	public class when_retrying_a_null_message : using_a_subscription
	{
		Establish context = () =>
			subscription.Dispose();

		Because of = () =>
			thrown = Catch.Exception(() => subscription.RetryMessage(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();

		static Exception thrown;
	}

	[Subject(typeof(RabbitSubscription))]
	public class when_disposing_a_subscription : using_a_subscription
	{
		Establish context = () =>
			mockRealSubscription.Setup(x => x.Dispose());

		Because of = () =>
			subscription.Dispose();

		private It should_dispose_the_subscription = () => 
			mockRealSubscription.Verify(x => x.Dispose(), Times.Once());
	}

	public abstract class using_a_subscription
	{
		Establish context = () =>
		{
			mockRealSubscription = new Mock<SubscriptionAdapter>();
			subscription = new RabbitSubscription(mockRealSubscription.Object);
		};

		protected static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(1);
		protected static Mock<SubscriptionAdapter> mockRealSubscription;
		protected static RabbitSubscription subscription;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169