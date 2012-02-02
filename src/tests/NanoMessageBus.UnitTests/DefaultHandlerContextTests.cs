#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
	using System;
	using System.Linq;
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

		It should_NOT_instruct_the_delivery_to_reattempt_the_message = () =>
			mockDelivery.Verify(x => x.Send(Moq.It.IsAny<ChannelEnvelope>()), Times.Never());
	}

	[Subject(typeof(DefaultHandlerContext))]
	public class when_deferring_a_message : with_a_handler_context
	{
		Establish context = () => mockDelivery
			.Setup(x => x.Send(Moq.It.IsAny<ChannelEnvelope>()))
			.Callback<ChannelEnvelope>(x =>
			{
				sent = x.Message;
				recipients = x.Recipients.ToArray();
			});

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

		static ChannelMessage sent;
		static Uri[] recipients;
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

		static readonly Mock<IDispatchContext> mockDispatch = new Mock<IDispatchContext>();
		static IDispatchContext dispatchContext;
	}

	[Subject(typeof(DefaultHandlerContext))]
	public class when_sending_an_envelope : with_a_handler_context
	{
		Because of = () =>
			handlerContext.Send(envelope);

		It should_invoke_the_underlying_channel = () =>
			mockDelivery.Verify(x => x.Send(envelope), Times.Once());

		static readonly ChannelEnvelope envelope = new Mock<ChannelEnvelope>().Object;
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

			mockDelivery = new Mock<IDeliveryContext>();
			mockDelivery.Setup(x => x.CurrentMessage).Returns(mockMessage.Object);
			mockDelivery.Setup(x => x.CurrentConfiguration).Returns(mockConfig.Object);
			mockDelivery.Setup(x => x.CurrentTransaction).Returns(mockTransaction.Object);
			mockDelivery.Setup(x => x.CurrentResolver).Returns(mockResolver.Object);

			thrown = null;

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

		protected static DefaultHandlerContext handlerContext;
		protected static Mock<IDeliveryContext> mockDelivery;
		protected static Mock<ChannelMessage> mockMessage;
		protected static Mock<IChannelTransaction> mockTransaction;
		protected static Mock<IDependencyResolver> mockResolver;
		protected static Mock<IChannelGroupConfiguration> mockConfig;
		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169