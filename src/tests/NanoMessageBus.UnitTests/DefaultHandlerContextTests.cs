﻿using FluentAssertions;

#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
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
			thrown.Should().BeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultHandlerContext))]
	public class when_constructing_a_new_handler_context : with_a_handler_context
	{
		It should_expose_the_active_state_from_the_underlying_channel = () =>
			handlerContext.Active.Should().Be(mockDelivery.Object.Active);

		It should_expose_the_underlying_delivery_message = () =>
			handlerContext.CurrentMessage.Should().Be(mockMessage.Object);

		It should_expose_the_underlying_delivery_transaction = () =>
			handlerContext.CurrentTransaction.Should().Be(mockTransaction.Object);

		It should_expose_the_underlying_delivery_configuration = () =>
			handlerContext.CurrentConfiguration.Should().Be(mockConfig.Object);

		It should_expose_the_underlying_delivery_dependency_resolver = () =>
			handlerContext.CurrentResolver.Should().Be(mockResolver.Object);

		It should_indicate_the_ability_to_continue_handling = () =>
			handlerContext.ContinueHandling.Should().BeTrue();
	}

	[Subject(typeof(DefaultHandlerContext))]
	public class when_dropping_a_message : with_a_handler_context
	{
		Because of = () =>
			handlerContext.DropMessage();

		It should_indicate_that_handling_should_be_discontinued = () =>
			handlerContext.ContinueHandling.Should().BeFalse();

		It should_NOT_dispatch_the_message_for_redelivery = () =>
			mockDelivery.Verify(x => x.PrepareDispatch(Moq.It.IsAny<object>(), null), Times.Never());
	}

	[Subject(typeof(DefaultHandlerContext))]
	public class when_the_underlying_delivery_becomes_inactive : with_a_handler_context
	{
		Establish context = () =>
			mockDelivery.Setup(x => x.Active).Returns(false);

		It should_indicate_that_handling_should_be_discontinued = () =>
			handlerContext.ContinueHandling.Should().BeFalse();
	}

	[Subject(typeof(DefaultHandlerContext))]
	public class when_deferring_a_message : with_a_handler_context
	{
		Because of = () =>
			handlerContext.DeferMessage();

		It should_indicate_that_handling_should_be_discontinued = () =>
			handlerContext.ContinueHandling.Should().BeFalse();

		It should_send_the_same_message_that_was_delivered = () =>
			sent.Should().Be(mockDelivery.Object.CurrentMessage);

		It should_send_the_message_the_loopback_address = () =>
			recipients[0].Should().Be(ChannelEnvelope.LoopbackAddress);

		It should_not_send_the_message_to_any_other_recipients = () =>
			recipients.Length.Should().Be(1);

		It should_only_invoke_the_behavior_once = () =>
			deliveryCount.Should().Be(1);
	}

	[Subject(typeof(DefaultHandlerContext))]
	public class when_deferring_a_message_multiple_times : with_a_handler_context
	{
		Establish context = () =>
			handlerContext.DeferMessage();

		Because of = () =>
			handlerContext.DeferMessage();

		It should_only_invoke_the_behavior_once = () =>
			deliveryCount.Should().Be(1);
	}

	[Subject(typeof(DefaultHandlerContext))]
	public class when_no_forwarding_recipients_are_specified : with_a_handler_context
	{
		Because of = () =>
			Try(() => handlerContext.ForwardMessage(null));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultHandlerContext))]
	public class when_an_empty_set_of_recipients_is_specified : with_a_handler_context
	{
		Because of = () =>
			Try(() => handlerContext.ForwardMessage(new Uri[] { null, null }));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ArgumentException>();
	}

	[Subject(typeof(DefaultHandlerContext))]
	public class when_forwarding_against_a_disposed_context : with_a_handler_context
	{
		Establish context = () =>
			handlerContext.Dispose();

		Because of = () =>
			Try(() => handlerContext.ForwardMessage(null));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(DefaultHandlerContext))]
	public class when_forwarding_the_current_message_to_a_set_of_recipients : with_a_handler_context
	{
		Because of = () =>
			handlerContext.ForwardMessage(forwardingRecipients);

		It should_continue_handling_the_current_message = () =>
			handlerContext.ContinueHandling.Should().BeTrue();

		It should_send_the_current_message = () =>
			mockDelivery.Verify(x => x.PrepareDispatch(mockMessage.Object, null), Times.Once());

		It should_send_to_each_recipient_specified = () =>
			recipients.SequenceEqual(forwardingRecipients).Should().BeTrue();

		static readonly Uri[] forwardingRecipients = new[]
		{
			new Uri("http://www.google.com/"), 
			new Uri("http://www.yahoo.com/"), 
			new Uri("http://xkcd.com/") 
		};
	}

	[Subject(typeof(DefaultHandlerContext))]
	public class when_preparing_to_dispatch : with_a_handler_context
	{
		Establish context = () => mockDelivery
			.Setup(x => x.PrepareDispatch("Hello, World!", null))
			.Returns(mockDispatch.Object);

		Because of = () =>
			dispatchContext = handlerContext.PrepareDispatch("Hello, World!");

		It should_invoke_the_underlying_channel = () =>
			mockDelivery.Verify(x => x.PrepareDispatch("Hello, World!", null), Times.Exactly(1));

		It should_return_the_reference_from_the_underlying_channel = () =>
			dispatchContext.Should().Be(mockDispatch.Object);

		static IDispatchContext dispatchContext;
	}

	[Subject(typeof(DefaultHandlerContext))]
	public class when_preparing_to_dispatch_with_an_alternate_channel : with_a_handler_context
	{
		Establish context = () => mockDelivery
			.Setup(x => x.PrepareDispatch("Hello, World!", mockAlternate.Object))
			.Returns(mockDispatch.Object);

		Because of = () =>
			dispatchContext = handlerContext.PrepareDispatch("Hello, World!", mockAlternate.Object);

		It should_invoke_the_underlying_channel = () =>
			mockDelivery.Verify(x => x.PrepareDispatch("Hello, World!", mockAlternate.Object), Times.Once());

		It should_return_the_reference_from_the_alternate_channel = () =>
			dispatchContext.Should().Be(mockDispatch.Object);

		static readonly Mock<IMessagingChannel> mockAlternate = new Mock<IMessagingChannel>();
		static IDispatchContext dispatchContext;
	}

	[Subject(typeof(DefaultHandlerContext))]
	public class when_disposing_the_handler_context : with_a_handler_context
	{
		Because of = () =>
			handlerContext.Dispose();

		It should_indicate_that_handling_should_be_discontinued = () =>
			handlerContext.ContinueHandling.Should().BeFalse();
	}

	[Subject(typeof(DefaultHandlerContext))]
	public class when_prepaing_to_dispatch_against_a_disposed_context : with_a_handler_context
	{
		Establish context = () =>
			handlerContext.Dispose();

		Because of = () =>
			Try(() => handlerContext.PrepareDispatch());

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(DefaultHandlerContext))]
	public class when_dropping_a_message_against_a_disposed_context : with_a_handler_context
	{
		Establish context = () =>
			handlerContext.Dispose();

		Because of = () =>
			Try(() => handlerContext.DropMessage());

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(DefaultHandlerContext))]
	public class when_deferring_a_message_against_a_disposed_context : with_a_handler_context
	{
		Establish context = () =>
			handlerContext.Dispose();

		Because of = () =>
			Try(() => handlerContext.DeferMessage());

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ObjectDisposedException>();
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
			mockDelivery.Setup(x => x.Active).Returns(true);
			mockDelivery.Setup(x => x.CurrentMessage).Returns(mockMessage.Object);
			mockDelivery.Setup(x => x.CurrentConfiguration).Returns(mockConfig.Object);
			mockDelivery.Setup(x => x.CurrentTransaction).Returns(mockTransaction.Object);
			mockDelivery.Setup(x => x.CurrentResolver).Returns(mockResolver.Object);
			mockDelivery.Setup(x => x.PrepareDispatch(Moq.It.IsAny<object>(), null)).Returns(mockDispatch.Object);

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
					deliveryCount++;
				});

			sent = null;
			recipients = null;
			thrown = null;
			deliveryCount = 0;
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
		protected static int deliveryCount;

		protected static ChannelMessage sent;
		protected static Uri[] recipients;

		private static List<Uri> queuedRecipients;
		private static ChannelMessage queuedMessage;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169, 414