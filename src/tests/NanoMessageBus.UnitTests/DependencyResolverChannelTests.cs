#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
	using System;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject(typeof(DependencyResolverChannel))]
	public class when_a_null_channel_is_provided : with_the_dependency_resolver_channel
	{
		Because of = () =>
			Try(() => Build(null, mockResolver.Object));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DependencyResolverChannel))]
	public class when_a_null_dependency_resolver_is_provided : with_the_dependency_resolver_channel
	{
		Because of = () =>
			Try(() => Build(mockWrappedChannel.Object, null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DependencyResolverChannel))]
	public class when_constructing_a_dependency_resolver_channel : with_the_dependency_resolver_channel
	{
		Establish context = () =>
		{
			mockWrappedChannel.Setup(x => x.CurrentMessage).Returns(new Mock<ChannelMessage>().Object);
			mockWrappedChannel.Setup(x => x.CurrentTransaction).Returns(new Mock<IChannelTransaction>().Object);
			mockWrappedChannel.Setup(x => x.CurrentConfiguration).Returns(new Mock<IChannelGroupConfiguration>().Object);
		};

		It should_expose_the_current_message_from_the_underlying_channel = () =>
			channel.CurrentMessage.ShouldEqual(mockWrappedChannel.Object.CurrentMessage);

		It should_expose_the_current_transaction_from_the_underlying_channel = () =>
			channel.CurrentTransaction.ShouldEqual(mockWrappedChannel.Object.CurrentTransaction);

		It should_expose_the_current_configuration_from_the_underlying_channel = () =>
			channel.CurrentConfiguration.ShouldEqual(mockWrappedChannel.Object.CurrentConfiguration);

		It should_expose_the_resolver_provided_during_construction = () =>
			channel.CurrentResolver.ShouldEqual(mockResolver.Object);
	}

	[Subject(typeof(DependencyResolverChannel))]
	public class when_invoking_send_on_the_channel : with_the_dependency_resolver_channel
	{
		Because of = () =>
			channel.Send(envelope);

		It should_directly_invoke_the_underlying_channel_using_the_envelope_provided = () =>
			mockWrappedChannel.Verify(x => x.Send(envelope), Times.Once());

		static readonly ChannelEnvelope envelope = new Mock<ChannelEnvelope>().Object;
	}

	[Subject(typeof(DependencyResolverChannel))]
	public class when_initiating_shutdown_on_the_channel : with_the_dependency_resolver_channel
	{
		Because of = () =>
			channel.BeginShutdown();

		It should_directly_invoke_the_underlying_channel = () =>
			mockWrappedChannel.Verify(x => x.BeginShutdown(), Times.Once());
	}

	[Subject(typeof(DependencyResolverChannel))]
	public class when_calling_receive_on_the_channel : with_the_dependency_resolver_channel
	{
		Because of = () =>
			channel.Receive(callback);

		It should_provide_a_delegate_to_the_underlying_channel = () =>
			mockWrappedChannel.Verify(x => x.Receive(Moq.It.IsAny<Action<IDeliveryContext>>()), Times.Once());

		It should_NOT_provide_the_exact_same_delegate_to_the_channel_without_wrapping_it = () =>
			mockWrappedChannel.Verify(x => x.Receive(callback), Times.Never());

		static readonly Action<IDeliveryContext> callback = context => { };
	}

	[Subject(typeof(DependencyResolverChannel))]
	public class when_the_receive_callback_specified_is_invoked : with_the_dependency_resolver_channel
	{
		Establish context = () =>
		{
			mockResolver.Setup(x => x.CreateNestedResolver()).Returns(mockNested.Object);

			mockOriginal.Setup(x => x.CurrentMessage).Returns(new Mock<ChannelMessage>().Object);
			mockOriginal.Setup(x => x.CurrentTransaction).Returns(new Mock<IChannelTransaction>().Object);
			mockOriginal.Setup(x => x.CurrentConfiguration).Returns(new Mock<IChannelGroupConfiguration>().Object);

			mockWrappedChannel
				.Setup(x => x.Receive(Moq.It.IsAny<Action<IDeliveryContext>>()))
				.Callback<Action<IDeliveryContext>>(x => x(mockOriginal.Object)); // it may not always be the underlying channel
		};

		Because of = () => channel.Receive(context =>
		{
			delivery = context;
			contextMessage = context.CurrentMessage;
			contextTransaction = context.CurrentTransaction;
			contextConfiguration = context.CurrentConfiguration;
			contextResolver = context.CurrentResolver;
			delivery.Send(sent);
		});

		It should_create_a_nested_resolver = () =>
			mockResolver.Verify(x => x.CreateNestedResolver(), Times.Once());

		It should_invoke_the_callback_specified_providing_itself_as_a_parameter = () =>
			delivery.ShouldEqual(channel);

		It should_temporarily_expose_the_nested_resolver_on_the_channel = () =>
			contextResolver.ShouldEqual(mockNested.Object);

		It should_expose_the_original_context_CurrentMessage = () => 
			contextMessage.ShouldEqual(mockOriginal.Object.CurrentMessage);

		It should_expose_the_original_context_CurrentTransaction = () =>
			contextTransaction.ShouldEqual(mockOriginal.Object.CurrentTransaction);

		It should_expose_the_original_context_CurrentConfiguration = () =>
			contextConfiguration.ShouldEqual(mockOriginal.Object.CurrentConfiguration);
		
		It should_send_through_the_original_context = () =>
			mockOriginal.Verify(x => x.Send(sent), Times.Once());

		It should_dispose_the_nested_resolver = () =>
			mockNested.Verify(x => x.Dispose());

		It should_revert_the_nested_resolver_back_to_the_constructed_resolver_upon_completion = () =>
			channel.CurrentResolver.ShouldEqual(mockResolver.Object);

		It should_revert_the_CurrentMessage_back_to_the_constructed_value_upon_completion = () =>
			channel.CurrentMessage.ShouldEqual(mockWrappedChannel.Object.CurrentMessage);

		It should_revert_the_CurrentTransaction_back_to_the_constructed_value_upon_completion = () =>
			channel.CurrentTransaction.ShouldEqual(mockWrappedChannel.Object.CurrentTransaction);

		It should_revert_the_CurrentConfiguration_back_to_the_constructed_value_upon_completion = () =>
			channel.CurrentConfiguration.ShouldEqual(mockWrappedChannel.Object.CurrentConfiguration);

		static IDeliveryContext delivery;
		static IDependencyResolver contextResolver;
		static IChannelTransaction contextTransaction;
		static IChannelGroupConfiguration contextConfiguration;
		static ChannelMessage contextMessage;
		static readonly Mock<IDeliveryContext> mockOriginal = new Mock<IDeliveryContext>();
		static readonly Mock<IDependencyResolver> mockNested = new Mock<IDependencyResolver>();
		static readonly ChannelEnvelope sent = new Mock<ChannelEnvelope>().Object;
	}

	[Subject(typeof(DependencyResolverChannel))]
	public class when_the_receive_callback_specified_is_invoked_and_throws_an_exception : with_the_dependency_resolver_channel
	{
		Establish context = () =>
		{
			mockResolver.Setup(x => x.CreateNestedResolver()).Returns(mockNested.Object);
			mockWrappedChannel
				.Setup(x => x.Receive(Moq.It.IsAny<Action<IDeliveryContext>>()))
				.Callback<Action<IDeliveryContext>>(x => x(mockWrappedChannel.Object));
		};

		Because of = () => Try(() => channel.Receive(context =>
		{
			throw new Exception();
		}));

		It should_dispose_the_nested_resolver = () =>
			mockNested.Verify(x => x.Dispose());

		It should_revert_the_nested_resolver_back_to_the_constructed_resolver_upon_completion = () =>
			channel.CurrentResolver.ShouldEqual(mockResolver.Object);

		It should_raise_the_exception = () =>
			thrown.ShouldNotBeNull();

		static readonly Mock<IDependencyResolver> mockNested = new Mock<IDependencyResolver>();
	}

	[Subject(typeof(DependencyResolverChannel))]
	public class when_disposing_the_channel : with_the_dependency_resolver_channel
	{
		Because of = () =>
			channel.Dispose();

		It should_dispose_the_underlying_channel = () =>
			mockWrappedChannel.Verify(x => x.Dispose(), Times.Once());

		It should_dispose_the_underlying_resolver = () =>
			mockResolver.Verify(x => x.Dispose(), Times.Once());
	}

	public abstract class with_the_dependency_resolver_channel
	{
		Establish context = () =>
		{
			mockResolver = new Mock<IDependencyResolver>();
			mockWrappedChannel = new Mock<IMessagingChannel>();
			Build();
		};
		protected static void Build()
		{
			Build(mockWrappedChannel.Object, mockResolver.Object);
		}
		protected static void Build(IMessagingChannel wrapped, IDependencyResolver resolver)
		{
			channel = new DependencyResolverChannel(wrapped, resolver);
		}
		protected static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		protected static DependencyResolverChannel channel;
		protected static Mock<IMessagingChannel> mockWrappedChannel;
		protected static Mock<IDependencyResolver> mockResolver;
		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169