#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject(typeof(DefaultHandlerContext))]
	public class when_providing_a_null_delivery_context : with_a_handler_context
	{
		Because of = () =>
			TryBuild(null);

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultHandlerContext))]
	public class when_constructing_a_new_handler_context : with_a_handler_context
	{
		It should_expose_the_underlying_delivery_group_name = () =>
			handlerContext.GroupName.ShouldEqual(GroupName);

		It should_expose_the_underlying_delivery_message = () =>
			handlerContext.CurrentMessage.ShouldEqual(mockMessage.Object);

		It should_expose_the_underlying_delivery_transaction = () =>
			handlerContext.CurrentTransaction.ShouldEqual(mockTransaction.Object);

		It should_expose_the_underlying_delivery_configuration = () =>
			handlerContext.CurrentConfiguration.ShouldEqual(mockConfig.Object);

		It should_expose_the_underlying_delivery_dependency_resolver = () =>
			handlerContext.CurrentResolver.ShouldEqual(mockResolver.Object);

		It should_indicate_the_ability_to_continue_handling = () =>
			handlerContext.ContinueHandling.ShouldBeTrue();
	}

	[Subject(typeof(DefaultHandlerContext))]
	public class when_dropping_a_message : with_a_handler_context
	{
		Because of = () =>
			handlerContext.DropMessage();

		It should_indicate_that_handling_should_be_discontinued = () =>
			handlerContext.ContinueHandling.ShouldBeFalse();

		It should_NOT_dispatch_the_message_for_redelivery = () =>
			mockDelivery.Verify(x => x.PrepareDispatch(Moq.It.IsAny<object>()), Times.Never());
	}

	[Subject(typeof(DefaultHandlerContext))]
	public class when_deferring_a_message : with_a_handler_context
	{
		Because of = () =>
			handlerContext.DeferMessage();

		It should_indicate_that_handling_should_be_discontinued = () =>
			handlerContext.ContinueHandling.ShouldBeFalse();

		It should_send_the_same_message_that_was_delivered = () =>
			sent.ShouldEqual(mockDelivery.Object.CurrentMessage);

		It should_send_the_message_the_loopback_address = () =>
			recipients[0].ShouldEqual(ChannelEnvelope.LoopbackAddress);

		It should_not_send_the_message_to_any_other_recipients = () =>
			recipients.Length.ShouldEqual(1);
	}

	[Subject(typeof(DefaultHandlerContext))]
	public class when_preparing_to_dispatch : with_a_handler_context
	{
		Establish context = () => mockDelivery
			.Setup(x => x.PrepareDispatch("Hello, World!"))
			.Returns(mockDispatch.Object);

		Because of = () =>
			dispatchContext = handlerContext.PrepareDispatch("Hello, World!");

		It should_invoke_the_underlying_channel = () =>
			mockDelivery.Verify(x => x.PrepareDispatch("Hello, World!"));

		It should_return_the_reference_from_the_underlying_channel = () =>
			dispatchContext.ShouldEqual(mockDispatch.Object);

		static IDispatchContext dispatchContext;
	}

	[Subject(typeof(DefaultHandlerContext))]
	public class when_disposing_the_handler_context : with_a_handler_context
	{
		Because of = () =>
			handlerContext.Dispose();

		It should_indicate_that_handling_should_be_discontinued = () =>
			handlerContext.ContinueHandling.ShouldBeFalse();
	}

	[Subject(typeof(DefaultHandlerContext))]
	public class when_prepaing_to_dispatch_against_a_disposed_context : with_a_handler_context
	{
		Establish context = () =>
			handlerContext.Dispose();

		Because of = () =>
			Try(() => handlerContext.PrepareDispatch());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(DefaultHandlerContext))]
	public class when_dropping_a_message_against_a_disposed_context : with_a_handler_context
	{
		Establish context = () =>
			handlerContext.Dispose();

		Because of = () =>
			Try(() => handlerContext.DropMessage());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(DefaultHandlerContext))]
	public class when_deferring_a_message_against_a_disposed_context : with_a_handler_context
	{
		Establish context = () =>
			handlerContext.Dispose();

		Because of = () =>
			Try(() => handlerContext.DeferMessage());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();
	}

	public abstract class with_a_handler_context
	{
		Establish context = () =>
		{
			mockMessage = new Mock<ChannelMessage>();
			mockConfig = new Mock<IChannelGroupConfiguration>();
			mockTransaction = new Mock<IChannelTransaction>();
			mockResolver = new Mock<IDependencyResolver>();
			mockDispatch = new Mock<IDispatchContext>();

			mockDelivery = new Mock<IDeliveryContext>();
			mockDelivery.Setup(x => x.GroupName).Returns(GroupName);
			mockDelivery.Setup(x => x.CurrentMessage).Returns(mockMessage.Object);
			mockDelivery.Setup(x => x.CurrentConfiguration).Returns(mockConfig.Object);
			mockDelivery.Setup(x => x.CurrentTransaction).Returns(mockTransaction.Object);
			mockDelivery.Setup(x => x.CurrentResolver).Returns(mockResolver.Object);
			mockDelivery.Setup(x => x.PrepareDispatch(Moq.It.IsAny<object>())).Returns(mockDispatch.Object);

			mockDispatch
				.Setup(x => x.WithMessage(Moq.It.IsAny<ChannelMessage>()))
				.Returns(mockDispatch.Object)
				.Callback<ChannelMessage>(x => queuedMessage = x);

			mockDispatch
				.Setup(x => x.WithRecipient(Moq.It.IsAny<Uri>()))
				.Returns(mockDispatch.Object)
				.Callback<Uri>(x => queuedRecipients.Add(x));

			mockDispatch
				.Setup(x => x.Send())
				.Callback(() =>
				{
					sent = queuedMessage;
					recipients = queuedRecipients.ToArray();
				});

			sent = null;
			recipients = null;
			thrown = null;
			queuedMessage = null;
			queuedRecipients = new List<Uri>();

			TryBuild(mockDelivery.Object);
		};
		protected static void TryBuild(IDeliveryContext delivery)
		{
			Try(() => handlerContext = new DefaultHandlerContext(delivery));
		}
		protected static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		protected const string GroupName = "Some Group Name";
		protected static DefaultHandlerContext handlerContext;
		protected static Mock<IDeliveryContext> mockDelivery;
		protected static Mock<ChannelMessage> mockMessage;
		protected static Mock<IChannelTransaction> mockTransaction;
		protected static Mock<IDispatchContext> mockDispatch;
		protected static Mock<IDependencyResolver> mockResolver;
		protected static Mock<IChannelGroupConfiguration> mockConfig;
		protected static Exception thrown;

		protected static ChannelMessage sent;
		protected static Uri[] recipients;

		private static List<Uri> queuedRecipients;
		private static ChannelMessage queuedMessage;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169