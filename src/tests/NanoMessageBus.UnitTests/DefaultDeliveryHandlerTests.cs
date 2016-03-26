using FluentAssertions;

#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
	using System;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject(typeof(DefaultDeliveryHandler))]
	public class when_a_null_routing_table_is_provided
	{
		Because of = () =>
			thrown = Catch.Exception(() => new DefaultDeliveryHandler(null));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ArgumentNullException>();

		static Exception thrown;
	}

	[Subject(typeof(DefaultDeliveryHandler))]
	public class when_a_null_delivery_is_provided
	{
		Because of = () =>
			thrown = Catch.Exception(() => handler.HandleAsync(null).Await());

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ArgumentNullException>();

		static readonly DefaultDeliveryHandler handler = new DefaultDeliveryHandler(new DefaultRoutingTable());
		static Exception thrown;
	}

	[Subject(typeof(DefaultDeliveryHandler))]
	public class when_handling_a_delivery
	{
		Establish context = () =>
		{
			mockMessage = new Mock<ChannelMessage>();

			mockRoutingTable = new Mock<IRoutingTable>();
			mockRoutingTable
                .Setup(x => x.Route(Moq.It.IsAny<DefaultHandlerContext>(), mockMessage.Object))
                .ReturnsAsync(1);

			mockDelivery = new Mock<IDeliveryContext>();
			mockDelivery.Setup(x => x.CurrentMessage).Returns(mockMessage.Object);

			handler = new DefaultDeliveryHandler(mockRoutingTable.Object);
		};

		Because of = () =>
			handler.HandleAsync(mockDelivery.Object).Await();

		It should_provide_the_current_message_to_the_routing_table = () =>
			mockRoutingTable.Verify(x => x.Route(Moq.It.IsAny<DefaultHandlerContext>(), mockMessage.Object), Times.Once());

		static DefaultDeliveryHandler handler;
		static Mock<IRoutingTable> mockRoutingTable;
		static Mock<ChannelMessage> mockMessage;
		static Mock<IDeliveryContext> mockDelivery;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169, 414