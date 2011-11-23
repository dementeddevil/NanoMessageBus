#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.RabbitChannel
{
	using System;
	using Machine.Specifications;
	using Moq;
	using RabbitMQ.Client;
	using It = Machine.Specifications.It;

	[Subject(typeof(RabbitSubscription))]
	public class when_attempting_to_receive_without_providing_a_callback : using_a_subscription
	{
		Because of = () =>
			exception = Catch.Exception(() => subscription.BeginReceive(DefaultTimeout, null));

		It should_throw_an_exception = () =>
			exception.ShouldBeOfType<ArgumentNullException>();

		static Exception exception;
	}

	[Subject(typeof(RabbitSubscription))]
	public class when_attempting_to_receive_does_not_yield_any_messages : using_a_subscription
	{
		Because of = () =>
			subscription.BeginReceive(DefaultTimeout, msg => invocations++);

		It should_not_invoke_the_callback_provided = () =>
			invocations.ShouldEqual(0);

		static int invocations;
	}

	[Subject(typeof(RabbitSubscription))]
	public class when_a_negative_timeout_is_specified : using_a_subscription
	{
		Because of = () =>
			exception = Catch.Exception(() => subscription.BeginReceive(ZeroTimeout, msg => { }));

		It should_throw_an_exception = () =>
			exception.ShouldBeOfType<ArgumentException>();

		static readonly TimeSpan ZeroTimeout = TimeSpan.FromMilliseconds(-1);
		static Exception exception;
	}

	public abstract class using_a_subscription
	{
		Establish context = () =>
		{
			mockChannel = new Mock<IModel>();
			transactionType = RabbitTransactionType.None;

			Initialize();
		};

		protected static void Initialize()
		{
			subscription = new RabbitSubscription(mockChannel.Object, DefaultQueueName, transactionType);
		}

		protected const string DefaultQueueName = "My input queue";
		protected static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(1);
		protected static Mock<IModel> mockChannel;
		protected static RabbitTransactionType transactionType;
		protected static RabbitSubscription subscription;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169