#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections.Generic;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject(typeof(PointOfOriginAuditor))]
	public class when_receiving_a_any_kind_of_message_delivery : using_the_point_of_origin_auditor
	{
		Because of = () =>
			Try(() => auditor.AuditReceive(null));

		It should_do_nothing = () =>
			thrown.ShouldBeNull();
	}

	[Subject(typeof(PointOfOriginAuditor))]
	public class when_sending_a_null_message : using_the_point_of_origin_auditor
	{
		Because of = () =>
			Try(() => auditor.AuditSend(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(PointOfOriginAuditor))]
	public class when_sending_a_message : using_the_point_of_origin_auditor
	{
		Establish context = () =>
			SystemTime.TimeResolver = () => SystemTime.EpochTime;

		Cleanup after = () =>
			SystemTime.TimeResolver = null;

		Because of = () =>
			Try(() => auditor.AuditSend(mockEnvelope.Object));

		It should_append_the_originating_machine_name_to_the_headers = () =>
			messageHeaders["x-audit-origin-host"].ShouldEqual(Environment.MachineName.ToLowerInvariant());

		It should_append_the_current_time_to_the_headers = () =>
			messageHeaders["x-audit-dispatch-stamp"].ShouldEqual(SystemTime.UtcNow.ToString("o"));
	}

	[Subject(typeof(PointOfOriginAuditor))]
	public class when_disposing_the_auditor : using_the_point_of_origin_auditor
	{
		Because of = () =>
			Try(auditor.Dispose);

		It should_do_nothing = () =>
			thrown.ShouldBeNull();
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
#pragma warning restore 169