#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.Channels
{
	using System;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject(typeof(PooledDispatchChannel))]
	public class when_a_null_connector_is_provided_to_the_pooled_dispatch_channel : using_the_pooled_dispatch_channel
	{
		Because of = () =>
			Try(() => new PooledDispatchChannel(null, mockChannel.Object, 1));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(PooledDispatchChannel))]
	public class when_a_null_channel_is_provided_to_the_pooled_dispatch_channel : using_the_pooled_dispatch_channel
	{
		Because of = () =>
			Try(() => new PooledDispatchChannel(mockConnector.Object, null, 1));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(PooledDispatchChannel))]
	public class when_a_negative_token_is_provided_during_construction : using_the_pooled_dispatch_channel
	{
		Because of = () =>
			Try(() => new PooledDispatchChannel(mockConnector.Object, mockChannel.Object, -1));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject(typeof(PooledDispatchChannel))]
	public class when_an_pooled_dispatch_channel_is_constructed : using_the_pooled_dispatch_channel
	{
		Establish context = () =>
		{
			mockChannel.Setup(x => x.GroupName).Returns("Hello, World!");
			mockChannel.Setup(x => x.CurrentMessage).Returns(new Mock<ChannelMessage>().Object);
			mockChannel.Setup(x => x.CurrentTransaction).Returns(new Mock<IChannelTransaction>().Object);
			mockChannel.Setup(x => x.CurrentResolver).Returns(new Mock<IDependencyResolver>().Object);
			mockChannel.Setup(x => x.CurrentConfiguration).Returns(new Mock<IChannelGroupConfiguration>().Object);
		};

		It should_expose_the_group_name_from_the_underlying_channel = () =>
			channel.GroupName.ShouldEqual(mockChannel.Object.GroupName);

		It should_ALWAYS_return_null = () =>
			channel.CurrentMessage.ShouldBeNull();

		It should_expose_the_current_resolver_from_the_underlying_channel = () =>
			channel.CurrentResolver.ShouldEqual(mockChannel.Object.CurrentResolver);

		It should_expose_the_current_transaction_from_the_underlying_channel = () =>
			channel.CurrentTransaction.ShouldEqual(mockChannel.Object.CurrentTransaction);

		It should_expose_the_current_configuration_from_the_underlying_channel = () =>
			channel.CurrentConfiguration.ShouldEqual(mockChannel.Object.CurrentConfiguration);
	}

	[Subject(typeof(PooledDispatchChannel))]
	public class when_disposing_the_pooled_dispatch_channel : using_the_pooled_dispatch_channel
	{
		Because of = () =>
			channel.Dispose();

		It should_pass_the_underlying_channel_and_token_back_to_the_connector = () =>
			mockConnector.Verify(x => x.Release(mockChannel.Object, ReleaseToken), Times.Once());

		It should_NOT_dispose_the_underlying_channel = () =>
			mockChannel.Verify(x => x.Dispose(), Times.Never());
	}

	[Subject(typeof(PooledDispatchChannel))]
	public class when_disposing_the_pooled_dispatch_channel_multiple_times : using_the_pooled_dispatch_channel
	{
		Establish context = () =>
			channel.Dispose();

		Because of = () =>
			channel.Dispose();

		It should_pass_a_self_reference_back_to_the_connector_exactly_once = () =>
			mockConnector.Verify(x => x.Release(mockChannel.Object, ReleaseToken), Times.Once());

		It should_never_dispose_the_underlying_channel = () =>
			mockChannel.Verify(x => x.Dispose(), Times.Never());
	}

	[Subject(typeof(PooledDispatchChannel))]
	public class when_preparing_a_message_for_dispatch : using_the_pooled_dispatch_channel
	{
		Establish context = () =>
			mockChannel.Setup(x => x.PrepareDispatch(MyMessage)).Returns(mockContext.Object);

		Because of = () =>
			dispatchContext = channel.PrepareDispatch(MyMessage);

		It should_provide_the_message_specified_to_the_underlying_channel = () =>
			mockChannel.Verify(x => x.PrepareDispatch(MyMessage), Times.Once());

		It should_return_the_dispatch_context_from_the_underlying_channel = () =>
			dispatchContext.ShouldEqual(mockContext.Object);

		const string MyMessage = "My message";
		static readonly Mock<IDispatchContext> mockContext = new Mock<IDispatchContext>();
		static IDispatchContext dispatchContext;
	}

	[Subject(typeof(PooledDispatchChannel))]
	public class when_trying_to_send_with_a_null_envelope : using_the_pooled_dispatch_channel
	{
		Because of = () =>
			Try(() => channel.Send(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(PooledDispatchChannel))]
	public class when_trying_to_send_against_a_disposed_channel : using_the_pooled_dispatch_channel
	{
		Establish context = () =>
			channel.Dispose();

		Because of = () =>
			Try(() => channel.Send(new Mock<ChannelEnvelope>().Object));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(PooledDispatchChannel))]
	public class when_sending_an_envelope_on_the_pooled_dispatch_channel : using_the_pooled_dispatch_channel
	{
		Because of = () =>
			channel.Send(envelope);

		It should_pass_the_envelope_to_the_underlying_channel_for_delivery = () =>
			mockChannel.Verify(x => x.Send(envelope), Times.Once());

		static readonly ChannelEnvelope envelope = new Mock<ChannelEnvelope>().Object;
	}

	[Subject(typeof(PooledDispatchChannel))]
	public class when_sending_an_envelope_results_in_a_ChannelConnectionException : using_the_pooled_dispatch_channel
	{
		Establish context = () =>
			mockChannel.Setup(x => x.Send(envelope)).Throws(new ChannelConnectionException());

		Because of = () =>
			Try(() => channel.Send(envelope));

		It should_tear_down_the_channel = () =>
			mockConnector.Verify(x => x.Teardown(mockChannel.Object, ReleaseToken), Times.Once());

		It should_rethrow_the_exception = () =>
			thrown.ShouldBeOfType<ChannelConnectionException>();

		static readonly ChannelEnvelope envelope = new Mock<ChannelEnvelope>().Object;
	}

	[Subject(typeof(PooledDispatchChannel))]
	public class when_initiating_shutdown_on_the_pooled_dispatch_channel : using_the_pooled_dispatch_channel
	{
		Because of = () =>
			Try(() => channel.BeginShutdown());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<NotSupportedException>();
	}

	[Subject(typeof(PooledDispatchChannel))]
	public class when_calling_receive_on_the_pooled_dispatch_channel : using_the_pooled_dispatch_channel
	{
		Because of = () =>
			Try(() => channel.Receive(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<NotSupportedException>();
	}

	public abstract class using_the_pooled_dispatch_channel
	{
		Establish context = () =>
		{
			mockChannel = new Mock<IMessagingChannel>();
			mockConnector = new Mock<PooledDispatchConnector>();

			channel = new PooledDispatchChannel(mockConnector.Object, mockChannel.Object, ReleaseToken);

			thrown = null;
		};
		protected static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		protected const int ReleaseToken = 42;
		protected static Mock<IMessagingChannel> mockChannel;
		protected static Mock<PooledDispatchConnector> mockConnector;
		protected static PooledDispatchChannel channel;
		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169