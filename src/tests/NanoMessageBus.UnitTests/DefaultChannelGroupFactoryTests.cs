#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
	using System;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject(typeof(DefaultChannelGroupFactory))]
	public class when_building_a_channel_with_a_null_connector : with_a_channel_group_factory
	{
		Because of = () =>
			Try(() => Factory.Build(null, config));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultChannelGroupFactory))]
	public class when_building_a_channel_with_a_null_configuration : with_a_channel_group_factory
	{
		Because of = () =>
			Try(() => Factory.Build(Connector, null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultChannelGroupFactory))]
	public class when_building_a_channel : with_a_channel_group_factory
	{
		Because of = () =>
			group = Factory.Build(Connector, config);

		It should_create_a_channel_group = () =>
			group.ShouldBeOfType<DefaultChannelGroup>();

		static IChannelGroup group;
	}

	public abstract class with_a_channel_group_factory
	{
		Establish context = () =>
		{
			var mock = new Mock<IChannelGroupConfiguration>();
			mock.Setup(x => x.MinWorkers).Returns(1);
			mock.Setup(x => x.MaxWorkers).Returns(1);

			config = mock.Object;
			thrown = null;
		};

		protected static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		protected static IChannelGroupConfiguration config;
		protected static readonly IChannelConnector Connector = new Mock<IChannelConnector>().Object;
		protected static readonly DefaultChannelGroupFactory Factory = new DefaultChannelGroupFactory();
		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169