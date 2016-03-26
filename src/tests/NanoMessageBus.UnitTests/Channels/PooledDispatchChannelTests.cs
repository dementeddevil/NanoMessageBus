using FluentAssertions;

#pragma warning disable 169, 414
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
			thrown.Should().BeOfType<ArgumentNullException>();
	}

	[Subject(typeof(PooledDispatchChannel))]
	public class when_a_null_channel_is_provided_to_the_pooled_dispatch_channel : using_the_pooled_dispatch_channel
	{
		Because of = () =>
			Try(() => new PooledDispatchChannel(mockConnector.Object, null, 1));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ArgumentNullException>();
	}

	[Subject(typeof(PooledDispatchChannel))]
	public class when_a_negative_token_is_provided_during_construction : using_the_pooled_dispatch_channel
	{
		Because of = () =>
			Try(() => new PooledDispatchChannel(mockConnector.Object, mockChannel.Object, -1));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ArgumentException>();
	}

	[Subject(typeof(PooledDispatchChannel))]
	public class when_a_pooled_dispatch_channel_is_constructed : using_the_pooled_dispatch_channel
	{
		Establish context = () =>
		{
			mockChannel.Setup(x => x.Active).Returns(true);
			mockChannel.Setup(x => x.CurrentMessage).Returns(new Mock<ChannelMessage>().Object);
			mockChannel.Setup(x => x.CurrentTransaction).Returns(new Mock<IChannelTransaction>().Object);
			mockChannel.Setup(x => x.CurrentResolver).Returns(new Mock<IDependencyResolver>().Object);
			mockChannel.Setup(x => x.CurrentConfiguration).Returns(new Mock<IChannelGroupConfiguration>().Object);
		};

		It should_ALWAYS_return_null = () =>
			channel.CurrentMessage.Should().BeNull();

		It should_expose_the_active_state_from_the_underlying_channel = () =>
			channel.Active.Should().Be(mockChannel.Object.Active);

		It should_expose_the_current_resolver_from_the_underlying_channel = () =>
			channel.CurrentResolver.Should().Be(mockChannel.Object.CurrentResolver);

		It should_expose_the_current_transaction_from_the_underlying_channel = () =>
			channel.CurrentTransaction.Should().Be(mockChannel.Object.CurrentTransaction);

		It should_expose_the_current_configuration_from_the_underlying_channel = () =>
			channel.CurrentConfiguration.Should().Be(mockChannel.Object.CurrentConfiguration);
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
		{
			var mockConfig = new Mock<IChannelGroupConfiguration>();
			mockConfig.Setup(x => x.DispatchTable).Returns(new Mock<IDispatchTable>().Object);
			mockChannel.Setup(x => x.CurrentConfiguration).Returns(mockConfig.Object);
		};

		Because of = () =>
			dispatchContext = channel.PrepareDispatch(MyMessage);

		It should_return_a_dispatch_context = () =>
			dispatchContext.Should().BeOfType<DefaultDispatchContext>();

		It should_contain_the_message_specified = () =>
			dispatchContext.MessageCount.Should().Be(1);

		It should_not_invoke_the_underlying_channel = () =>
			mockChannel.Verify(x => x.PrepareDispatch(MyMessage, null), Times.Never());

		const string MyMessage = "My message";
		static readonly Mock<IDispatchContext> mockContext = new Mock<IDispatchContext>();
		static IDispatchContext dispatchContext;
	}

	[Subject(typeof(PooledDispatchChannel))]
	public class when_preparing_for_dispatch_without_a_message : using_the_pooled_dispatch_channel
	{
		Establish context = () =>
		{
			var mockConfig = new Mock<IChannelGroupConfiguration>();
			mockConfig.Setup(x => x.DispatchTable).Returns(new Mock<IDispatchTable>().Object);
			mockChannel.Setup(x => x.CurrentConfiguration).Returns(mockConfig.Object);
		};

		Because of = () =>
			dispatchContext = channel.PrepareDispatch();

		It should_return_a_dispatch_context = () =>
			dispatchContext.Should().BeOfType<DefaultDispatchContext>();

		It should_not_contain_any_messages = () =>
			dispatchContext.MessageCount.Should().Be(0);

		It should_not_invoke_the_underlying_channel = () =>
			mockChannel.Verify(x => x.PrepareDispatch(null, null), Times.Never());

		static readonly Mock<IDispatchContext> mockContext = new Mock<IDispatchContext>();
		static IDispatchContext dispatchContext;
	}

	[Subject(typeof(PooledDispatchChannel))]
	public class when_preparing_for_a_dispatch_with_an_alternate_channel : using_the_pooled_dispatch_channel
	{
		Establish context = () =>
		{
			var mockConfig = new Mock<IChannelGroupConfiguration>();
			mockConfig.Setup(x => x.DispatchTable).Returns(new Mock<IDispatchTable>().Object);
			mockChannel.Setup(x => x.CurrentConfiguration).Returns(mockConfig.Object);
			mockAlternateChannel.Setup(x => x.CurrentConfiguration).Returns(mockConfig.Object);
		};

		Because of = () =>
			dispatchContext = channel.PrepareDispatch(MyMessage, mockAlternateChannel.Object);

		It should_return_a_dispatch_context = () =>
			dispatchContext.Should().BeOfType<DefaultDispatchContext>();

		It should_invoke_the_underlying_channel_providing_the_alternate_channel = () =>
			mockChannel.Verify(x => x.PrepareDispatch(MyMessage, mockAlternateChannel.Object), Times.Once());

		const string MyMessage = "My message";
		static readonly Mock<IDispatchContext> mockContext = new Mock<IDispatchContext>();
		static IDispatchContext dispatchContext;
		static readonly Mock<IMessagingChannel> mockAlternateChannel = new Mock<IMessagingChannel>();
	}

	[Subject(typeof(PooledDispatchChannel))]
	public class when_trying_to_send_with_a_null_envelope : using_the_pooled_dispatch_channel
	{
		Because of = () =>
			Try(() => channel.SendAsync(null).Await());

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ArgumentNullException>();
	}

	[Subject(typeof(PooledDispatchChannel))]
	public class when_trying_to_send_against_a_disposed_channel : using_the_pooled_dispatch_channel
	{
		Establish context = () =>
			channel.Dispose();

		Because of = () =>
			Try(() => channel.SendAsync(new Mock<ChannelEnvelope>().Object).Await());

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(PooledDispatchChannel))]
	public class when_sending_an_envelope_on_the_pooled_dispatch_channel : using_the_pooled_dispatch_channel
	{
		Because of = () =>
			channel.SendAsync(envelope).Await();

		It should_pass_the_envelope_to_the_underlying_channel_for_delivery = () =>
			mockChannel.Verify(x => x.SendAsync(envelope), Times.Once());

		static readonly ChannelEnvelope envelope = new Mock<ChannelEnvelope>().Object;
	}

	[Subject(typeof(PooledDispatchChannel))]
	public class when_sending_an_envelope_results_in_a_ChannelConnectionException : using_the_pooled_dispatch_channel
	{
		Establish context = () =>
			mockChannel.Setup(x => x.SendAsync(envelope)).Throws(new ChannelConnectionException());

		Because of = () =>
			Try(() => channel.SendAsync(envelope).Await());

		It should_tear_down_the_channel = () =>
			mockConnector.Verify(x => x.Teardown(mockChannel.Object, ReleaseToken), Times.Once());

		It should_rethrow_the_exception = () =>
			thrown.Should().BeOfType<ChannelConnectionException>();

		static readonly ChannelEnvelope envelope = new Mock<ChannelEnvelope>().Object;
	}

	[Subject(typeof(PooledDispatchChannel))]
	public class when_initiating_shutdown_on_the_pooled_dispatch_channel : using_the_pooled_dispatch_channel
	{
		Because of = () =>
			Try(() => channel.ShutdownAsync().Await());

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<NotSupportedException>();
	}

	[Subject(typeof(PooledDispatchChannel))]
	public class when_calling_receive_on_the_pooled_dispatch_channel : using_the_pooled_dispatch_channel
	{
		Because of = () =>
			Try(() => channel.ReceiveAsync(null).Await());

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<NotSupportedException>();
	}

	public abstract class using_the_pooled_dispatch_channel
	{
		Establish context = () =>
		{
			mockChannel = new Mock<IMessagingChannel>();
			mockChannel
				.Setup(x => x.PrepareDispatch(Moq.It.IsAny<object>(), Moq.It.IsAny<IMessagingChannel>()))
				.Returns<object, IMessagingChannel>((msg, alt) =>
				{
					var ctx = new DefaultDispatchContext(alt);
					return msg == null ? ctx : ctx.WithMessage(msg);
				});

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
#pragma warning restore 169, 414