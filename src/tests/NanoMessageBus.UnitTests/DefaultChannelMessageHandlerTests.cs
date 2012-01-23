#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
	using System;
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
			mockContext.Setup(x => x.ContinueHandling).Returns(() => continueProcessing);
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

		static bool continueProcessing = true;
	}


	public abstract class with_a_channel_message_handler
	{
		Establish context = () =>
		{
			handler = null;
			thrown = null;
			mockContext = new Mock<IHandlerContext>();
			mockContext.Setup(x => x.ContinueHandling).Returns(true);
			mockRoutes = new Mock<IRoutingTable>();
			mockMessage = new Mock<ChannelMessage>();
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
		protected static Mock<IRoutingTable> mockRoutes;
		protected static Mock<ChannelMessage> mockMessage;
		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169