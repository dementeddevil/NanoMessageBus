#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
	using System;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject(typeof(DependencyResolverConnector))]
	public class when_constructing_with_a_null_connector : with_the_dependency_resolver_connector
	{
		Because of = () =>
			Try(() => Build(null, mockResolver.Object));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DependencyResolverConnector))]
	public class when_constructing_with_a_null_resolver : with_the_dependency_resolver_connector
	{
		Because of = () =>
			Try(() => Build(mockWrappedConnector.Object, null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DependencyResolverConnector))]
	public class when_constructing_a_connector : with_the_dependency_resolver_connector
	{
		Establish context = () =>
		{
			mockWrappedConnector.Setup(x => x.CurrentState).Returns(ConnectionState.Open);
			mockWrappedConnector.Setup(x => x.ChannelGroups).Returns(new IChannelGroupConfiguration[0]);
		};

		It should_expose_the_underlying_connection_state_of_the_wrapped_connector = () =>
			connector.CurrentState.ShouldEqual(mockWrappedConnector.Object.CurrentState);

		It should_expose_the_underlying_set_of_channel_group_configuration_of_the_wrapped_connector = () =>
			connector.ChannelGroups.ShouldEqual(mockWrappedConnector.Object.ChannelGroups);
	}

	[Subject(typeof(DependencyResolverConnector))]
	public class when_resolving_a_channel : with_the_dependency_resolver_connector
	{
		Because of = () =>
			connected = connector.Connect("some key");

		It should_connect_to_the_channel_using_the_underlying_connector = () =>
			mockWrappedConnector.Verify(x => x.Connect("some key"), Times.Once());

		It should_create_a_new_resolver = () =>
			mockResolver.Verify(x => x.CreateNestedResolver(Moq.It.IsAny<string>()), Times.Once());

		It should_return_a_reference_to_a_DependencyResolverChannel = () =>
			connected.ShouldBeOfType<DependencyResolverChannel>();

		static IMessagingChannel connected;
	}

	[Subject(typeof(DependencyResolverConnector))]
	public class when_resolving_the_channel_throws_an_exception : with_the_dependency_resolver_connector
	{
		Establish context = () =>
			mockResolver.Setup(x => x.CreateNestedResolver(Moq.It.IsAny<string>())).Throws(toThrow);

		Because of = () =>
			Try(() => connector.Connect("some key"));

		It should_dispose_the_created_channel = () =>
			mockWrappedChannel.Verify(x => x.Dispose(), Times.Once());

		It should_rethrow_the_exception = () =>
			thrown.ShouldEqual(toThrow);

		static readonly Exception toThrow = new Exception("some exception");
	}

	[Subject(typeof(DependencyResolverConnector))]
	public class when_disposing_the_connector : with_the_dependency_resolver_connector
	{
		Because of = () =>
			connector.Dispose();

		It should_dispose_the_underlying_connector = () =>
			mockWrappedConnector.Verify(x => x.Dispose(), Times.Once());

		It should_dispose_the_underlying_resolver = () =>
			mockResolver.Verify(x => x.Dispose(), Times.Once());
	}

	public abstract class with_the_dependency_resolver_connector
	{
		Establish context = () =>
		{
			mockResolver = new Mock<IDependencyResolver>();
			mockWrappedChannel = new Mock<IMessagingChannel>();
			mockWrappedConnector = new Mock<IChannelConnector>();
			mockWrappedConnector
				.Setup(x => x.Connect(Moq.It.IsAny<string>()))
				.Returns(mockWrappedChannel.Object);
			Build();
		};
		protected static void Build()
		{
			Build(mockWrappedConnector.Object, mockResolver.Object);
		}
		protected static void Build(IChannelConnector wrapped, IDependencyResolver resolver)
		{
			connector = new DependencyResolverConnector(wrapped, resolver);
		}
		protected static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		protected static DependencyResolverConnector connector;
		protected static Mock<IDependencyResolver> mockResolver;
		protected static Mock<IChannelConnector> mockWrappedConnector;
		protected static Mock<IMessagingChannel> mockWrappedChannel;
		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169