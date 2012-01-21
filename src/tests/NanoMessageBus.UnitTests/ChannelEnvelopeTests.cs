#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject(typeof(ChannelEnvelope))]
	public class when_constructing_a_new_channel_envelope
	{
		Because of = () =>
			envelope = new ChannelEnvelope(message, recipients);

		It should_contain_a_reference_to_the_message_provided = () =>
			envelope.Message.ShouldBeTheSameAs(message);

		It should_contain_all_non_null_recipients_specified = () =>
			envelope.Recipients.Count.ShouldEqual(recipients.Count(x => x != null));

		It should_contain_all_the_recipients_specified = () =>
			envelope.Recipients.ToList().ForEach(uri => recipients.Contains(uri).ShouldBeTrue());

		static readonly ChannelMessage message = new Mock<ChannelMessage>().Object;
		static readonly ICollection<Uri> recipients = new HashSet<Uri>
		{
			ChannelEnvelope.LoopbackAddress, new Uri("msmq://testing"), null
		};
		static ChannelEnvelope envelope;
	}

	[Subject(typeof(ChannelEnvelope))]
	public class when_constructing_an_envelope_without_providing_a_message
	{
		Because of = () =>
			thrown = Catch.Exception(() => new ChannelEnvelope(null, recipients));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();

		static readonly ICollection<Uri> recipients = new HashSet<Uri> { ChannelEnvelope.LoopbackAddress };
		static Exception thrown;
	}

	[Subject(typeof(ChannelEnvelope))]
	public class when_constructing_an_envelope_while_providing_a_null_set_of_recipients
	{
		Because of = () =>
			thrown = Catch.Exception(() => new ChannelEnvelope(message, null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();

		static readonly ChannelMessage message = new Mock<ChannelMessage>().Object;
		static Exception thrown;
	}

	[Subject(typeof(ChannelEnvelope))]
	public class when_constructing_an_envelope_while_providing_an_empty_set_of_recipients
	{
		Because of = () =>
			thrown = Catch.Exception(() => new ChannelEnvelope(message, new List<Uri>()));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentException>();

		static readonly ChannelMessage message = new Mock<ChannelMessage>().Object;
		static Exception thrown;
	}

	[Subject(typeof(ChannelEnvelope))]
	public class when_attempting_to_modify_the_recipient_collection
	{
		Establish context = () =>
			envelope = new ChannelEnvelope(message, recipients);

		Because of = () =>
			thrown = Catch.Exception(() => envelope.Recipients.Clear());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<NotSupportedException>();

		static readonly ChannelMessage message = new Mock<ChannelMessage>().Object;
		static readonly ICollection<Uri> recipients = new[] { ChannelEnvelope.LoopbackAddress };
		static ChannelEnvelope envelope;
		static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169