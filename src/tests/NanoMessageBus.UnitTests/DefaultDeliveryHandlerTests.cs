#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
	using System;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject(typeof(DefaultDeliveryHandler))]
	public class when_a_null_routing_table_is_provided : with_a_delivery_handler
	{
		Because of = () =>
			thrown = Catch.Exception(() => new DefaultDeliveryHandler(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();

		static Exception thrown;
	}

	[Subject(typeof(DefaultDeliveryHandler))]
	public class when_a_null_delivery_is_provided : with_a_delivery_handler
	{
		Because of = () =>
			thrown = Catch.Exception(() => handler.Handle(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();

		static readonly DefaultDeliveryHandler handler = new DefaultDeliveryHandler(new DefaultRoutingTable());
		static Exception thrown;
	}

	[Subject(typeof(DefaultDeliveryHandler))]
	public class when_handling_a_delivery : with_a_delivery_handler
	{
		Establish context = () =>
		{
			mockMessage = new Mock<ChannelMessage>();

			mockRoutingTable = new Mock<IRoutingTable>();
			mockRoutingTable.Setup(x => x.Route(Moq.It.IsAny<DefaultHandlerContext>(), mockMessage.Object));

			mockTransaction = new Mock<IChannelTransaction>();
			mockTransaction.Setup(x => x.Commit());

			mockDelivery = new Mock<IDeliveryContext>();
			mockDelivery.Setup(x => x.CurrentTransaction).Returns(mockTransaction.Object);
			mockDelivery.Setup(x => x.CurrentMessage).Returns(mockMessage.Object);

			handler = new DefaultDeliveryHandler(mockRoutingTable.Object);
		};

		Because of = () =>
			handler.Handle(mockDelivery.Object);

		It should_provide_the_current_message_to_the_routing_table = () =>
			mockRoutingTable.Verify(x => x.Route(Moq.It.IsAny<DefaultHandlerContext>(), mockMessage.Object), Times.Once());

		It should_commit_the_transtion_on_the_delivery_provided = () =>
			mockTransaction.Verify(x => x.Commit(), Times.Once());

		static DefaultDeliveryHandler handler;
		static Mock<IRoutingTable> mockRoutingTable;
		static Mock<ChannelMessage> mockMessage;
		static Mock<IChannelTransaction> mockTransaction;
		static Mock<IDeliveryContext> mockDelivery;
	}

	public abstract class with_a_delivery_handler
	{
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169