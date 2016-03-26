using FluentAssertions;

#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
	using System;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject(typeof(TransactionScopeDeliveryHandler))]
	public class when_no_delivery_handler_is_provided
	{
		Because of = () =>
			thrown = Catch.Exception(() => new TransactionScopeDeliveryHandler(null));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ArgumentNullException>();

		static Exception thrown;
	}

	[Subject(typeof(TransactionScopeDeliveryHandler))]
	public class when_handle_is_invoked
	{
		Establish context = () =>
		{
			mockHandler = new Mock<IDeliveryHandler>();
			mockHandler.Setup(x => x.HandleAsync(Delivery));

			handler = new TransactionScopeDeliveryHandler(mockHandler.Object);
		};

		Because of = () =>
			handler.HandleAsync(Delivery).Await();

		It should_invoke_the_underlying_handle_method = () =>
			mockHandler.Verify(x => x.HandleAsync(Delivery), Times.Once());

		static TransactionScopeDeliveryHandler handler;
		static Mock<IDeliveryHandler> mockHandler;
		static readonly IDeliveryContext Delivery = new Mock<IDeliveryContext>().Object;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169, 414