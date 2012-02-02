#pragma warning disable 169
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
			TryBuild(mockContext.Object, null);

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultChannelMessageHandler))]
	public class when_handling_a_message : with_a_channel_message_handler
	{
		Establish context = () =>
			mockMessage.Setup(x => x.Messages).Returns(new object[] { "1", 2, 3.0 });

		Because of = () =>
			handler.Handle(mockMessage.Object);

		It should_route_each_logical_message_back_to_the_underlying_routing_table = () =>
		{
			mockRoutes.Verify(x => x.Route(mockContext.Object, "1"), Times.Once());
			mockRoutes.Verify(x => x.Route(mockContext.Object, 2), Times.Once());
			mockRoutes.Verify(x => x.Route(mockContext.Object, 3.0), Times.Once());
		};
	}

	[Subject(typeof(DefaultChannelMessageHandler))]
	public class when_the_context_discontinues_a_message_while_being_handled : with_a_channel_message_handler
	{
		Establish context = () =>
		{
			mockMessage.Setup(x => x.Messages).Returns(new object[] { "1", 2 });
			mockRoutes
				.Setup(x => x.Route(mockContext.Object, "1"))
				.Callback<IHandlerContext, object>((ctx, msg) => continueProcessing = false);
		};

		Because of = () =>
			handler.Handle(mockMessage.Object);

		It should_route_each_logical_message_back_to_the_underlying_routing_table = () =>
		{
			mockRoutes.Verify(x => x.Route(mockContext.Object, "1"), Times.Once());
			mockRoutes.Verify(x => x.Route(mockContext.Object, 2), Times.Never());
		};
	}

	[Subject(typeof(DefaultChannelMessageHandler))]
	public class when_no_routes_exist_for_any_logical_message : with_a_channel_message_handler
	{
		Establish context = () =>
			mockMessage.Setup(x => x.Messages).Returns(new object[] { 0 });

		Because of = () =>
			handler.Handle(mockMessage.Object);

		It should_put_the_incoming_channel_message_into_a_channel_envelope = () =>
			sentMessage.ShouldEqual(mockMessage.Object);

		It should_send_the_envelope_to_the_dead_letter_address = () =>
			recipients[0].ShouldEqual(ChannelEnvelope.DeadLetterAddress);
	}

	[Subject(typeof(DefaultChannelMessageHandler))]
	public class when_instructed_not_to_continue_processing : with_a_channel_message_handler
	{
		Establish context = () =>
		{
			continueProcessing = false;
			mockMessage.Setup(x => x.Messages).Returns(new object[] { 0 });
		};

		Because of = () =>
			handler.Handle(mockMessage.Object);

		It should_NEVER_send_the_incoming_channel_message_to_the_dead_letter_address = () =>
			sentMessage.ShouldBeNull();
	}

	[Subject(typeof(DefaultChannelMessageHandler))]
	public class when_specific_logical_messages_are_not_handled : with_a_channel_message_handler
	{
		Establish context = () =>
		{
			mockMessage.Setup(x => x.Messages).Returns(new object[] { 1, "2", 3.0, 4.0M });
			mockMessage.Setup(x => x.MessageId).Returns(messageId);
			mockMessage.Setup(x => x.CorrelationId).Returns(correlationId);
			mockMessage.Setup(x => x.ReturnAddress).Returns(new Uri("http://www.google.com/"));
			mockMessage.Setup(x => x.Headers).Returns(new Dictionary<string, string>());

			mockRoutes.Setup(x => x.Route(mockContext.Object, 1)).Returns(1); // message is handled
			mockRoutes.Setup(x => x.Route(mockContext.Object, 3.0)).Returns(1); // message is handled
		};

		Because of = () =>
			handler.Handle(mockMessage.Object);

		It should_put_the_ignored_messages_into_a_channel_envelope = () =>
			sentMessage.Messages.SequenceEqual(new object[] { "2", 4.0M }).ShouldBeTrue();

		It should_add_a_unique_message_identifier_to_the_outgoing_channel_message = () =>
			sentMessage.MessageId.ShouldNotEqual(mockMessage.Object.MessageId);

		It should_add_the_incoming_correlation_identifier_to_the_outgoing_channel_message = () =>
			sentMessage.CorrelationId.ShouldEqual(mockMessage.Object.CorrelationId);

		It should_add_the_incoming_return_address_to_the_outgoing_channel_message = () =>
			sentMessage.ReturnAddress.ShouldEqual(mockMessage.Object.ReturnAddress);

		It should_add_the_incoming_message_headers_to_the_outgoing_channel_message = () =>
			ReferenceEquals(sentMessage.Headers, mockMessage.Object.Headers).ShouldBeTrue();

		It should_forward_the_channel_envelope_to_the_dead_letter_address = () =>
			recipients[0].ShouldEqual(ChannelEnvelope.DeadLetterAddress);

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
			mockContext = new Mock<IHandlerContext>();
			mockDelivery = new Mock<IDeliveryContext>();
			mockRoutes = new Mock<IRoutingTable>();
			mockMessage = new Mock<ChannelMessage>();
			continueProcessing = true;

			mockContext.Setup(x => x.ContinueHandling).Returns(() => continueProcessing);
			mockContext
				.Setup(x => x.Send(Moq.It.IsAny<ChannelEnvelope>()))
				.Callback<ChannelEnvelope>(x => mockDelivery.Object.Send(x));

			mockDelivery
				.Setup(x => x.Send(Moq.It.IsAny<ChannelEnvelope>()))
				.Callback<ChannelEnvelope>(x =>
				{
					sentMessage = x.Message;
					recipients = x.Recipients.ToArray();
				});

			TryBuild(mockContext.Object, mockRoutes.Object);
		};
		protected static void TryBuild(IHandlerContext context, IRoutingTable routes)
		{
			Try(() => handler = new DefaultChannelMessageHandler(context, routes));
		}
		protected static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		protected static DefaultChannelMessageHandler handler;
		protected static Mock<IHandlerContext> mockContext;
		protected static Mock<IDeliveryContext> mockDelivery;
		protected static Mock<IRoutingTable> mockRoutes;
		protected static Mock<ChannelMessage> mockMessage;
		protected static Exception thrown;
		protected static bool continueProcessing;

		protected static ChannelMessage sentMessage;
		protected static Uri[] recipients;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169