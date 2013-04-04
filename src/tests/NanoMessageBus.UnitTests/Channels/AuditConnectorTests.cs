#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject(typeof(AuditConnector))]
	public class when_a_null_connector_is_specified : using_the_audit_connector
	{
		Because of = () =>
			Try(() => new AuditConnector(null, null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(AuditConnector))]
	public class when_a_null_callback_is_specified : using_the_audit_connector
	{
		Because of = () =>
			Try(() => new AuditConnector(mockConnector.Object, null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(AuditConnector))]
	public class when_disposing_the_audit_connector : using_the_audit_connector
	{
		Because of = () =>
			connector.Dispose();

		It should_dispose_the_underlying_connector = () =>
			mockConnector.Verify(x => x.Dispose(), Times.Once());
	}

	[Subject(typeof(AuditConnector))]
	public class when_retrieving_the_current_connection_state : using_the_audit_connector
	{
		Establish context = () =>
			mockConnector.Setup(x => x.CurrentState).Returns(ConnectionState.Unauthorized);

		Because of = () =>
			connectionState = connector.CurrentState;

		It should_invoke_the_underlying_connector = () =>
			connectionState.ShouldEqual(ConnectionState.Unauthorized);

		static ConnectionState connectionState;
	}

	[Subject(typeof(AuditConnector))]
	public class when_retrieving_the_current_set_of_channel_group_configurations : using_the_audit_connector
	{
		Establish context = () => mockConnector.Setup(x => x.ChannelGroups).Returns(new[]
		{
			new Mock<IChannelGroupConfiguration>().Object,
			new Mock<IChannelGroupConfiguration>().Object
		});

		Because of = () =>
			configs = connector.ChannelGroups.ToArray();

		It should_invoke_the_underlying_connector = () =>
			configs.SequenceEqual(mockConnector.Object.ChannelGroups).ShouldBeTrue();

		static IChannelGroupConfiguration[] configs;
	}

	[Subject(typeof(AuditConnector))]
	public class when_a_new_channel_is_established : using_the_audit_connector
	{
		Establish context = () =>
			auditors.Add(new Mock<IMessageAuditor>());

		Because of = () =>
			connectedChannel = connector.Connect(ChannelGroupName);

		It should_invoke_the_underlying_connector = () =>
			mockConnector.Verify(x => x.Connect(ChannelGroupName));

		It should_resolve_the_set_of_auditors = () =>
			calls.ShouldEqual(1);

		It should_provide_the_resolved_channel_to_the_auditor_callback = () =>
			callbackChannel.ShouldEqual(mockChannel.Object);

		It should_wrap_over_the_channel = () =>
			connectedChannel.ShouldBeOfType<AuditChannel>();

		static IMessagingChannel connectedChannel;
	}

	[Subject(typeof(AuditConnector))]
	public class when_no_auditors_exist : using_the_audit_connector
	{
		Because of = () =>
			connectedChannel = connector.Connect(ChannelGroupName);

		It should_invoke_the_underlying_connector = () =>
			mockConnector.Verify(x => x.Connect(ChannelGroupName));

		It should_resolve_the_set_of_auditors = () =>
			calls.ShouldEqual(1);

		It should_provide_the_resolved_channel_to_the_auditor_callback = () =>
			callbackChannel.ShouldEqual(mockChannel.Object);

		It should_return_the_undecorated_channel = () =>
			connectedChannel.ShouldEqual(mockChannel.Object);

		static IMessagingChannel connectedChannel;
	}

	[Subject(typeof(AuditConnector))]
	public class when_more_channels_are_establish_with_no_auditors : using_the_audit_connector
	{
		Establish context = () =>
			connector.Connect(ChannelGroupName); // no auditors resolved

		Because of = () =>
			connectedChannel = connector.Connect(ChannelGroupName); // shouldn't invoke callback

		It should_return_the_undecorated_channel = () =>
			connectedChannel.ShouldEqual(mockChannel.Object);

		It should_never_again_invoke_the_auditor_callback = () =>
			calls.ShouldEqual(1);

		static IMessagingChannel connectedChannel;
	}

	public abstract class using_the_audit_connector
	{
		Establish context = () =>
		{
			mockConnector = new Mock<IChannelConnector>();
			mockChannel = new Mock<IMessagingChannel>();
			mockConnector.Setup(x => x.Connect(ChannelGroupName)).Returns(mockChannel.Object);

			auditors.Clear();
			thrown = null;
			calls = 0;
			callbackChannel = null;

			connector = new AuditConnector(mockConnector.Object, c =>
			{
				calls++;
				callbackChannel = c;
				return auditors.Select(x => x.Object);
			});
		};

		protected static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		protected const string ChannelGroupName = "Test Group";
		protected static AuditConnector connector;
		protected static Mock<IChannelConnector> mockConnector;
		protected static Mock<IMessagingChannel> mockChannel;
		protected static Exception thrown;
		protected static IMessagingChannel callbackChannel;
		protected static int calls;
		protected static ICollection<Mock<IMessageAuditor>> auditors = new List<Mock<IMessageAuditor>>();
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169, 414