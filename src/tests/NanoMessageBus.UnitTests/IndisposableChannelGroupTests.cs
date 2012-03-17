#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
	using System;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject(typeof(IndisposableChannelGroup))]
	public class when_a_null_channel_group_is_provided : with_an_indisposable_channel_group
	{
		Because of = () =>
			Try(() => new IndisposableChannelGroup(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(IndisposableChannelGroup))]
	public class when_the_object_is_constructed : with_an_indisposable_channel_group
	{
		Establish context = () =>
			mockInner.Setup(x => x.DispatchOnly).Returns(true);

		It should_expose_the_underlying_channel_group_properties = () =>
			group.DispatchOnly.ShouldBeTrue();
	}

	[Subject(typeof(IndisposableChannelGroup))]
	public class when_initialize_is_invoked : with_an_indisposable_channel_group
	{
		Because of = () =>
			group.Initialize();

		It should_invoke_initialize_on_the_underlying_group = () =>
			mockInner.Verify(x => x.Initialize(), Times.Once());
	}

	[Subject(typeof(IndisposableChannelGroup))]
	public class when_begin_dispatch_is_invoked : with_an_indisposable_channel_group
	{
		Establish context = () =>
			mockInner.Setup(x => x.BeginDispatch(dispatchAction)).Returns(true);

		Because of = () =>
			queued = group.BeginDispatch(dispatchAction);

		It should_invoke_begin_dispatch_on_the_underlying_group = () =>
			mockInner.Verify(x => x.BeginDispatch(dispatchAction), Times.Once());

		It should_return_the_value_from_the_underlying_group_when_begin_dispatch_is_called = () =>
			queued.ShouldBeTrue();

		static bool queued;
	}

	[Subject(typeof(IndisposableChannelGroup))]
	public class when_begin_receive_is_invoked : with_an_indisposable_channel_group
	{
		Because of = () =>
			group.BeginReceive(deliveryAction);

		It should_invoke_begin_receive_on_the_underlying_group = () =>
			mockInner.Verify(x => x.BeginReceive(deliveryAction), Times.Once());
	}

	[Subject(typeof(IndisposableChannelGroup))]
	public class when_disposee_is_invoked : with_an_indisposable_channel_group
	{
		It should_NOT_invoke_dispose_on_the_underlying_group = () =>
			mockInner.Verify(x => x.Dispose(), Times.Never());
	}

	public abstract class with_an_indisposable_channel_group
	{
		Establish context = () =>
		{
			mockInner = new Mock<IChannelGroup>();
			thrown = null;

			group = new IndisposableChannelGroup(mockInner.Object);

			// code coverage
			deliveryAction(null);
			dispatchAction(null);
		};
		protected static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		protected static IndisposableChannelGroup group;
		protected static Mock<IChannelGroup> mockInner;
		protected static Exception thrown;

		protected static readonly Action<IDeliveryContext> deliveryAction = x => { };
		protected static readonly Action<IDispatchContext> dispatchAction = x => { };
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169