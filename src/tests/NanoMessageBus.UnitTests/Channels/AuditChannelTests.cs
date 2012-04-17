#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject(typeof(AuditChannel))]
	public class when_a_null_channel_is_provided_to_the_audit_channel : using_the_audit_channel
	{
		Because of = () =>
			Try(() => new AuditChannel(null, mockAuditors.Select(x => x.Object).ToArray()));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(AuditChannel))]
	public class when_a_null_set_of_auditor_is_provided_to_the_audit_channel : using_the_audit_channel
	{
		Because of = () =>
			Try(() => new AuditChannel(mockChannel.Object, null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(AuditChannel))]
	public class when_an_empty_set_of_auditor_is_provided_to_the_audit_channel : using_the_audit_channel
	{
		Because of = () =>
			Try(() => new AuditChannel(mockChannel.Object, new IMessageAuditor[0]));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject(typeof(AuditChannel))]
	public class when_an_audit_channel_is_constructed : using_the_audit_channel
	{
		Establish context = () =>
		{
			mockChannel.Setup(x => x.Active).Returns(true);
			mockChannel.Setup(x => x.CurrentMessage).Returns(new Mock<ChannelMessage>().Object);
			mockChannel.Setup(x => x.CurrentTransaction).Returns(new Mock<IChannelTransaction>().Object);
			mockChannel.Setup(x => x.CurrentResolver).Returns(new Mock<IDependencyResolver>().Object);
			mockChannel.Setup(x => x.CurrentConfiguration).Returns(new Mock<IChannelGroupConfiguration>().Object);
		};

		It should_expose_the_active_state_from_the_underlying_channel = () =>
			channel.Active.ShouldEqual(mockChannel.Object.Active);

		It should_expose_the_current_message_from_the_underlying_channel = () =>
			channel.CurrentMessage.ShouldEqual(mockChannel.Object.CurrentMessage);

		It should_expose_the_current_resolver_from_the_underlying_channel = () =>
			channel.CurrentResolver.ShouldEqual(mockChannel.Object.CurrentResolver);

		It should_expose_the_current_transaction_from_the_underlying_channel = () =>
			channel.CurrentTransaction.ShouldEqual(mockChannel.Object.CurrentTransaction);

		It should_expose_the_current_configuration_from_the_underlying_channel = () =>
			channel.CurrentConfiguration.ShouldEqual(mockChannel.Object.CurrentConfiguration);
	}

	[Subject(typeof(AuditChannel))]
	public class when_disposing_the_audit_channel : using_the_audit_channel
	{
		Because of = () =>
			channel.Dispose();

		It should_dispose_each_of_the_auditor = () =>
			mockAuditors.ForEach(mock => mock.Verify(x => x.Dispose(), Times.Once()));

		It should_dispose_the_underlying_channel = () =>
			mockChannel.Verify(x => x.Dispose(), Times.Once());
	}

	[Subject(typeof(AuditChannel))]
	public class when_preparing_a_dispatch : using_the_audit_channel
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
			dispatchContext.ShouldBeOfType<DefaultDispatchContext>();

		It should_contain_the_message_specified = () =>
			dispatchContext.MessageCount.ShouldEqual(1);

		It should_not_invoke_the_underlying_channel = () =>
			mockChannel.Verify(x => x.PrepareDispatch(MyMessage), Times.Never());

		const string MyMessage = "My message";
		static readonly Mock<IDispatchContext> mockContext = new Mock<IDispatchContext>();
		static IDispatchContext dispatchContext;
	}

	[Subject(typeof(AuditChannel))]
	public class when_preparing_a_dispatch_without_a_message : using_the_audit_channel
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
			dispatchContext.ShouldBeOfType<DefaultDispatchContext>();

		It should_not_contain_any_messages = () =>
			dispatchContext.MessageCount.ShouldEqual(0);

		It should_not_invoke_the_underlying_channel = () =>
			mockChannel.Verify(x => x.PrepareDispatch(null), Times.Never());

		static readonly Mock<IDispatchContext> mockContext = new Mock<IDispatchContext>();
		static IDispatchContext dispatchContext;
	}

	[Subject(typeof(AuditChannel))]
	public class when_attempting_to_send_with_a_null_envelope : using_the_audit_channel
	{
		Because of = () =>
			Try(() => channel.Send(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(AuditChannel))]
	public class when_attempting_to_send_against_a_disposed_channel : using_the_audit_channel
	{
		Establish context = () =>
			channel.Dispose();

		Because of = () =>
			Try(() => channel.Send(new Mock<ChannelEnvelope>().Object));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(AuditChannel))]
	public class when_sending_an_envelope_on_the_channel : using_the_audit_channel
	{
		Establish context = () =>
		{
			var mockTransaction = new Mock<IChannelTransaction>();
			mockChannel.Setup(x => x.CurrentTransaction).Returns(mockTransaction.Object);
			mockTransaction.Setup(x => x.Register(Moq.It.IsAny<Action>())).Callback<Action>(auditAction =>
			{
				registered = true;
				auditAction();
			});
		};

		Because of = () =>
			channel.Send(envelope);

		It should_register_the_audit_to_be_run_with_the_ambient_transaction = () =>
			registered.ShouldBeTrue();

		It should_provide_the_envelope_to_each_auditor = () => // the transaction commit causes this to occur
			mockAuditors.ForEach(mock => mock.Verify(x => x.AuditSend(envelope), Times.Once()));

		It should_pass_the_envelope_to_the_underlying_channel_for_delivery = () =>
			mockChannel.Verify(x => x.Send(envelope), Times.Once());

		static bool registered;
		static readonly ChannelEnvelope envelope = new Mock<ChannelEnvelope>().Object;
	}

	[Subject(typeof(AuditChannel))]
	public class when_initiating_shutdown_on_the_audit_channel : using_the_audit_channel
	{
		Because of = () =>
			channel.BeginShutdown();

		It should_call_the_underlying_channel = () =>
			mockChannel.Verify(x => x.BeginShutdown());
	}

	[Subject(typeof(AuditChannel))]
	public class when_calling_receive_on_the_audit_channel : using_the_audit_channel
	{
		Because of = () =>
			channel.Receive(callback);

		It should_provide_a_delegate_to_the_underlying_channel = () =>
			mockChannel.Verify(x => x.Receive(Moq.It.IsAny<Action<IDeliveryContext>>()), Times.Once());

		It should_NOT_provide_the_exact_same_delegate_to_the_channel_without_wrapping_it = () =>
			mockChannel.Verify(x => x.Receive(callback), Times.Never());

		static readonly Action<IDeliveryContext> callback = context => { };
	}

	[Subject(typeof(AuditChannel))]
	public class when_the_specified_receive_callback_is_invoked_on_the_audit_channel : using_the_audit_channel
	{
		Establish context = () =>
		{
			mockOriginal.Setup(x => x.CurrentMessage).Returns(new Mock<ChannelMessage>().Object);
			mockOriginal.Setup(x => x.CurrentTransaction).Returns(new Mock<IChannelTransaction>().Object);
			mockOriginal.Setup(x => x.CurrentConfiguration).Returns(new Mock<IChannelGroupConfiguration>().Object);

			mockChannel
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
		});

		It should_provide_the_original_unwrapped_delivery_context_to_each_of_the_configured_auditors = () =>
			mockAuditors.ForEach(mock => mock.Verify(x => x.AuditReceive(mockOriginal.Object), Times.Once()));

		It should_invoke_the_callback_specified_providing_itself_as_a_parameter = () =>
			delivery.ShouldEqual(channel);

		It should_expose_the_original_context_CurrentResolver = () =>
			contextResolver.ShouldEqual(mockOriginal.Object.CurrentResolver);

		It should_expose_the_original_context_CurrentMessage = () =>
			contextMessage.ShouldEqual(mockOriginal.Object.CurrentMessage);

		It should_expose_the_original_context_CurrentTransaction = () =>
			contextTransaction.ShouldEqual(mockOriginal.Object.CurrentTransaction);

		It should_expose_the_original_context_CurrentConfiguration = () =>
			contextConfiguration.ShouldEqual(mockOriginal.Object.CurrentConfiguration);

		It should_revert_the_nested_resolver_back_to_the_constructed_value_upon_completion = () =>
			channel.CurrentResolver.ShouldEqual(mockChannel.Object.CurrentResolver);

		It should_revert_the_CurrentMessage_back_to_the_constructed_value_upon_completion = () =>
			channel.CurrentMessage.ShouldEqual(mockChannel.Object.CurrentMessage);

		It should_revert_the_CurrentTransaction_back_to_the_constructed_value_upon_completion = () =>
			channel.CurrentTransaction.ShouldEqual(mockChannel.Object.CurrentTransaction);

		It should_revert_the_CurrentConfiguration_back_to_the_constructed_value_upon_completion = () =>
			channel.CurrentConfiguration.ShouldEqual(mockChannel.Object.CurrentConfiguration);

		static IDeliveryContext delivery;
		static string groupName;
		static IDependencyResolver contextResolver;
		static IChannelTransaction contextTransaction;
		static IChannelGroupConfiguration contextConfiguration;
		static ChannelMessage contextMessage;
		static readonly Mock<IDeliveryContext> mockOriginal = new Mock<IDeliveryContext>();
	}

	public abstract class using_the_audit_channel
	{
		Establish context = () =>
		{
			mockChannel = new Mock<IMessagingChannel>();
			mockAuditors.Add(new Mock<IMessageAuditor>());
			channel = new AuditChannel(mockChannel.Object, mockAuditors.Select(x => x.Object).ToArray());

			thrown = null;
		};
		protected static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		protected static List<Mock<IMessageAuditor>> mockAuditors = new List<Mock<IMessageAuditor>>();
		protected static Mock<IMessagingChannel> mockChannel;
		protected static AuditChannel channel;
		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169