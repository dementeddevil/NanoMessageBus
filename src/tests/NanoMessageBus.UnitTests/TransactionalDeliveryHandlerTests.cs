﻿using System.Threading.Tasks;
using FluentAssertions;

#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
	using System;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject(typeof(TransactionalDeliveryHandler))]
	public class when_a_null_inner_delivery_handler_is_provided
	{
		Because of = () =>
			thrown = Catch.Exception(() => new TransactionalDeliveryHandler(null));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ArgumentNullException>();

		static Exception thrown;
	}

	[Subject(typeof(TransactionalDeliveryHandler))]
	public class when_a_null_delivery_is_provided_to_the_delivery_handler
	{
		Because of = () =>
			thrown = Catch.Exception(() => handler.HandleAsync(null));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ArgumentNullException>();

		static readonly TransactionalDeliveryHandler handler =
			new TransactionalDeliveryHandler(new Mock<IDeliveryHandler>().Object);
		static Exception thrown;
	}

	[Subject(typeof(TransactionalDeliveryHandler))]
	public class when_handling_a_delivery_to_the_handler
	{
		Establish context = () =>
		{
			mockTransaction = new Mock<IChannelTransaction>();
			mockTransaction.Setup(x => x.Commit());

			mockDelivery = new Mock<IDeliveryContext>();
			mockDelivery.Setup(x => x.CurrentTransaction).Returns(mockTransaction.Object);

			mockInnerHandler = new Mock<IDeliveryHandler>();
			mockInnerHandler.Setup(x => x.HandleAsync(mockDelivery.Object)).Returns(Task.FromResult(true));

			handler = new TransactionalDeliveryHandler(mockInnerHandler.Object);
		};

		Because of = () =>
			handler.HandleAsync(mockDelivery.Object).Await();

		It should_provide_the_delivery_to_the_inner_handler = () =>
			mockInnerHandler.Verify(x => x.HandleAsync(mockDelivery.Object), Times.Once());

		It should_commit_the_transtion_on_the_delivery_provided = () =>
			mockTransaction.Verify(x => x.Commit(), Times.Once());

		static TransactionalDeliveryHandler handler;
		static Mock<IChannelTransaction> mockTransaction;
		static Mock<IDeliveryHandler> mockInnerHandler;
		static Mock<IDeliveryContext> mockDelivery;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169, 414