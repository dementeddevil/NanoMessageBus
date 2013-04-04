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

	[Subject(typeof(DefaultChannelMessageHandler))]
	public class when_constructing_with_a_null_context : with_a_channel_message_handler
	{
		Because of = () =>
			TryBuild(null, mockRoutes.Object);

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultChannelMessageHandler))]
	public class when_constructing_with_a_null_routing_table : with_a_channel_message_handler
	{
		Because of = () =>
			TryBuild(mockHandlerContext.Object, null);

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultChannelMessageHandler))]
	public class when_handling_a_message : with_a_channel_message_handler
	{
		Establish context = () =>
			deliveredMessage = BuildMessage(new object[] { "1", 2, 3.0 });

		Because of = () =>
			handler.Handle(deliveredMessage);

		It should_route_each_logical_message_back_to_the_underlying_routing_table = () =>
		{
			mockRoutes.Verify(x => x.Route(mockHandlerContext.Object, "1"), Times.Once());
			mockRoutes.Verify(x => x.Route(mockHandlerContext.Object, 2), Times.Once());
			mockRoutes.Verify(x => x.Route(mockHandlerContext.Object, 3.0), Times.Once());
		};

		It should_reset_the_current_message_index = () =>
			deliveredMessage.ActiveIndex.ShouldEqual(-1);
	}

	[Subject(typeof(DefaultChannelMessageHandler))]
	public class when_handling_a_message_throws_an_exception : with_a_channel_message_handler
	{
		Establish context = () =>
		{
			deliveredMessage = BuildMessage(new object[] { "1", 2, 3.0 });
			mockRoutes.Setup(x => x.Route(mockHandlerContext.Object, "1")).Throws(new Exception());
		};

		Because of = () =>
			Try(() => handler.Handle(deliveredMessage));

		It should_reset_the_current_message_index = () =>
			deliveredMessage.ActiveIndex.ShouldEqual(-1);
	}

	[Subject(typeof(DefaultChannelMessageHandler))]
	public class when_the_context_discontinues_a_message_while_being_handled : with_a_channel_message_handler
	{
		Establish context = () =>
		{
			deliveredMessage = BuildMessage(new object[] { "1", 2 });
			mockRoutes
				.Setup(x => x.Route(mockHandlerContext.Object, "1"))
				.Callback<IHandlerContext, object>((ctx, msg) => continueProcessing = false);
		};

		Because of = () =>
			handler.Handle(deliveredMessage);

		It should_route_each_logical_message_back_to_the_underlying_routing_table = () =>
		{
			mockRoutes.Verify(x => x.Route(mockHandlerContext.Object, "1"), Times.Once());
			mockRoutes.Verify(x => x.Route(mockHandlerContext.Object, 2), Times.Never());
		};

		It should_reset_the_current_message_index = () =>
			deliveredMessage.ActiveIndex.ShouldEqual(-1);
	}

	[Subject(typeof(DefaultChannelMessageHandler))]
	public class when_no_handlers_exist_for_any_logical_message : with_a_channel_message_handler
	{
		Establish context = () =>
			deliveredMessage = BuildMessage(new object[] { 0 });

		Because of = () =>
			handler.Handle(deliveredMessage);

		It should_put_the_incoming_channel_message_into_a_channel_envelope = () =>
			sentMessage.ShouldEqual(deliveredMessage);

		It should_send_the_envelope_to_the_unhandled_message_address = () =>
			recipients[0].ShouldEqual(ChannelEnvelope.UnhandledMessageAddress);

		It should_reset_the_current_message_index = () =>
			deliveredMessage.ActiveIndex.ShouldEqual(-1);
	}

	[Subject(typeof(DefaultChannelMessageHandler))]
	public class when_instructed_not_to_continue_processing : with_a_channel_message_handler
	{
		Establish context = () =>
		{
			continueProcessing = false;
			deliveredMessage = BuildMessage(new object[] { 0 });
		};

		Because of = () =>
			handler.Handle(deliveredMessage);

		It should_NEVER_send_the_incoming_channel_message_to_the_dead_letter_address = () =>
			sentMessage.ShouldBeNull();

		It should_reset_the_current_message_index = () =>
			deliveredMessage.ActiveIndex.ShouldEqual(-1);
	}

	[Subject(typeof(DefaultChannelMessageHandler))]
	public class when_specific_logical_messages_are_not_handled : with_a_channel_message_handler
	{
		Establish context = () =>
		{
			deliveredMessage = new ChannelMessage(
				messageId,
				correlationId,
				new Uri("http://www.google.com/"),
				new Dictionary<string, string>(),
				new object[] { 1, "2", 3.0, 4.0M });

			mockRoutes.Setup(x => x.Route(mockHandlerContext.Object, 1)).Returns(1); // message is handled
			mockRoutes.Setup(x => x.Route(mockHandlerContext.Object, 3.0)).Returns(1); // message is handled
		};

		Because of = () =>
			handler.Handle(deliveredMessage);

		It should_put_the_ignored_messages_into_a_channel_message = () =>
			sentMessage.Messages.SequenceEqual(new object[] { "2", 4.0M }).ShouldBeTrue();

		It should_add_a_unique_message_identifier_to_the_outgoing_channel_message = () =>
			sentMessage.MessageId.ShouldNotEqual(deliveredMessage.MessageId);

		It should_add_the_incoming_correlation_identifier_to_the_outgoing_channel_message = () =>
			sentMessage.CorrelationId.ShouldEqual(deliveredMessage.CorrelationId);

		It should_add_the_incoming_return_address_to_the_outgoing_channel_message = () =>
			sentMessage.ReturnAddress.ShouldEqual(deliveredMessage.ReturnAddress);

		It should_add_the_incoming_message_headers_to_the_outgoing_channel_message = () =>
			ReferenceEquals(sentMessage.Headers, deliveredMessage.Headers).ShouldBeTrue();

		It should_forward_the_channel_envelope_to_the_unhandled_message_address = () =>
			recipients[0].ShouldEqual(ChannelEnvelope.UnhandledMessageAddress);

		It should_reset_the_current_message_index = () =>
			deliveredMessage.ActiveIndex.ShouldEqual(-1);

		static readonly Guid messageId = Guid.NewGuid();
		static readonly Guid correlationId = Guid.NewGuid();
	}

	public abstract class with_a_channel_message_handler
	{
		Establish context = () =>
		{
			handler = null;
			thrown = null;
			sentMessage = null;
			recipients = null;
			queuedMessage = null;
			queuedRecipients = new List<Uri>();

			mockHandlerContext = new Mock<IHandlerContext>();
			mockDelivery = new Mock<IDeliveryContext>();
			mockDispatchContext = new Mock<IDispatchContext>();
			mockRoutes = new Mock<IRoutingTable>();
			continueProcessing = true;

			mockHandlerContext
				.Setup(x => x.ContinueHandling)
				.Returns(() => continueProcessing);

			mockHandlerContext
				.Setup(x => x.PrepareDispatch(Moq.It.IsAny<object>(), null))
				.Returns(mockDispatchContext.Object);

			mockDispatchContext
				.Setup(x => x.WithMessage(Moq.It.IsAny<ChannelMessage>()))
				.Returns(mockDispatchContext.Object)
				.Callback<ChannelMessage>(x => queuedMessage = x);

			mockDispatchContext
				.Setup(x => x.WithRecipient(Moq.It.IsAny<Uri>()))
				.Returns(mockDispatchContext.Object)
				.Callback<Uri>(x => queuedRecipients.Add(x));

			mockDispatchContext
				.Setup(x => x.Send())
				.Callback(() =>
				{
					sentMessage = queuedMessage;
					recipients = queuedRecipients.ToArray();
				});

			TryBuild(mockHandlerContext.Object, mockRoutes.Object);
		};
		protected static void TryBuild(IHandlerContext context, IRoutingTable routes)
		{
			Try(() => handler = new DefaultChannelMessageHandler(context, routes));
		}
		protected static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}
		protected static ChannelMessage BuildMessage(IEnumerable<object> messages)
		{
			return new ChannelMessage(
				Guid.NewGuid(),
				Guid.NewGuid(),
				new Uri("http://localhost"),
				new Dictionary<string, string>(), 
				messages);
		}

		protected static DefaultChannelMessageHandler handler;
		protected static Mock<IHandlerContext> mockHandlerContext;
		protected static Mock<IDispatchContext> mockDispatchContext;
		protected static Mock<IDeliveryContext> mockDelivery;
		protected static Mock<IRoutingTable> mockRoutes;
		protected static Exception thrown;
		protected static bool continueProcessing;

		protected static ChannelMessage deliveredMessage;
		protected static ChannelMessage sentMessage;
		protected static Uri[] recipients;

		private static List<Uri> queuedRecipients;
		private static ChannelMessage queuedMessage;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169, 414