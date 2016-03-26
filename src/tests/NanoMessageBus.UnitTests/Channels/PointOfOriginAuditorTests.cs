using FluentAssertions;

#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject(typeof(PointOfOriginAuditor))]
	public class when_receiving_a_null_delivery : using_the_point_of_origin_auditor
	{
		Because of = () =>
			Try(() => auditor.AuditReceive(null));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ArgumentNullException>();
	}

	[Subject(typeof(PointOfOriginAuditor))]
	public class when_receiving_a_delivery_with_a_dispatch_stamp_header : using_the_point_of_origin_auditor
	{
		Establish context = () =>
		{
			message = new ChannelMessage(Guid.Empty, Guid.Empty, null, new Dictionary<string, string>(), null);
			message.Headers["x-audit-dispatched"] = Dispatched.ToIsoString();

			mockDelivery = new Mock<IDeliveryContext>();
			mockDelivery.Setup(x => x.CurrentMessage).Returns(message);
		};

		Because of = () =>
			auditor.AuditReceive(mockDelivery.Object);

		It should_correct_the_origin_dispatch_date_of_the_message = () =>
			message.Dispatched.Should().Be(Dispatched);

		static Mock<IDeliveryContext> mockDelivery;
		static ChannelMessage message;
		static readonly DateTime Dispatched = SystemTime.UtcNow;
	}

	[Subject(typeof(PointOfOriginAuditor))]
	public class when_sending_a_null_message : using_the_point_of_origin_auditor
	{
		Because of = () =>
			Try(() => auditor.AuditSend(null, null));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ArgumentNullException>();
	}

	[Subject(typeof(PointOfOriginAuditor))]
	public class when_sending_a_message : using_the_point_of_origin_auditor
	{
		Establish context = () =>
			SystemTime.TimeResolver = () => SystemTime.EpochTime;

		Cleanup after = () =>
			SystemTime.TimeResolver = null;

		Because of = () =>
			auditor.AuditSend(mockEnvelope.Object, null);

		It should_append_the_originating_machine_name_to_the_headers = () =>
			messageHeaders["x-audit-origin-host"].Should().Be(Environment.MachineName.ToLowerInvariant());

		It should_append_the_current_time_to_the_headers = () =>
			messageHeaders["x-audit-dispatched"].Should().Be(SystemTime.UtcNow.ToString("o"));

		It should_append_the_originating_process_id_to_the_headers = () =>
			messageHeaders["x-audit-origin-process-id"].Should().Be(Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture));

		It should_append_the_originating_process_name_to_the_headers = () =>
			messageHeaders["x-audit-origin-process-name"].Should().Be(Process.GetCurrentProcess().ProcessName);
	}

	[Subject(typeof(PointOfOriginAuditor))]
	public class when_sending_a_channel_message : using_the_point_of_origin_auditor
	{
		Establish context = () =>
		{
			mockEnvelope.Setup(x => x.State).Returns(mockMessage.Object);
			messageHeaders["x-audit-origin-host"] = "a";
			messageHeaders["x-audit-dispatched"] = "b";
			messageHeaders["x-audit-origin-process-id"] = "c";
			messageHeaders["x-audit-origin-process-name"] = "d";
		};

		Because of = () =>
			auditor.AuditSend(mockEnvelope.Object, null);

		It should_NOT_modify_the_originating_machine_header = () =>
			messageHeaders["x-audit-origin-host"].Should().Be("a");

		It should_NOT_modify_the_originating_dispatch_stamp = () =>
			messageHeaders["x-audit-dispatched"].Should().Be("b");

		It should_NOT_modify_the_originating_process_id = () =>
			messageHeaders["x-audit-origin-process-id"].Should().Be("c");

		It should_NOT_modify_the_originating_process_name = () =>
			messageHeaders["x-audit-origin-process-name"].Should().Be("d");
	}

	[Subject(typeof(PointOfOriginAuditor))]
	public class when_sending_the_incoming_channel_message : using_the_point_of_origin_auditor
	{
		Establish context = () =>
		{
			mockDelivery = new Mock<IDeliveryContext>();
			mockDelivery.Setup(x => x.CurrentMessage).Returns(mockMessage.Object);
		};

		Because of = () =>
			auditor.AuditSend(mockEnvelope.Object, mockDelivery.Object);

		It should_NOT_modify_the_incoming_machine_header = () =>
			messageHeaders.ContainsKey("x-audit-origin-host").Should().BeFalse();

		It should_NOT_modify_the_incoming_dispatch_stamp = () =>
			messageHeaders.ContainsKey("x-audit-dispatched").Should().BeFalse();

		static Mock<IDeliveryContext> mockDelivery;
	}

	[Subject(typeof(PointOfOriginAuditor))]
	public class when_disposing_the_auditor : using_the_point_of_origin_auditor
	{
		Because of = () =>
			Try(auditor.Dispose);

		It should_do_nothing = () =>
			thrown.Should().BeNull();
	}

	public abstract class using_the_point_of_origin_auditor
	{
		Establish context = () =>
		{
			mockEnvelope = new Mock<ChannelEnvelope>();
			mockMessage = new Mock<ChannelMessage>();
			messageHeaders = new Dictionary<string, string>();
			mockEnvelope.Setup(x => x.Message).Returns(mockMessage.Object);
			mockMessage.Setup(x => x.Headers).Returns(messageHeaders);

			thrown = null;

			auditor = new PointOfOriginAuditor();
		};
		protected static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		protected static PointOfOriginAuditor auditor;
		protected static Mock<ChannelMessage> mockMessage;
		protected static Mock<ChannelEnvelope> mockEnvelope;
		protected static IDictionary<string, string> messageHeaders;
		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169, 414