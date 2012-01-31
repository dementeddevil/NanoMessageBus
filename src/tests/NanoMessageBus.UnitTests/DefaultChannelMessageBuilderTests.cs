#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Machine.Specifications;
	using It = Machine.Specifications.It;

	[Subject(typeof(DefaultChannelMessageBuilder))]
	public class when_a_channel_message_is_built : with_a_message_builder
	{
		Establish context = () =>
		{
			headers["null key"] = null;
			headers["empty key"] = string.Empty;
			headers["populated key"] = "some value";
		};

		Because of = () =>
			built = builder.Build(correlationId, returnAddress, headers, messages);

		It should_have_a_message_id = () =>
			built.MessageId.ShouldNotEqual(Guid.Empty);

		It should_have_the_correlation_id_specified = () =>
			built.CorrelationId.ShouldEqual(correlationId);

		It should_have_the_return_address_specified = () =>
			built.ReturnAddress.ShouldEqual(returnAddress);

		It should_have_a_reference_the_same_headers_object_instance = () =>
			built.Headers.ShouldEqual(headers);

		It should_have_same_number_of_headers_as_the_set_provided = () =>
			built.Headers.Count.ShouldEqual(headers.Count);

		It should_have_the_same_header_values_for_each_header_provided = () =>
			built.Headers.Keys.ToList().ForEach(x => built.Headers[x].ShouldEqual(headers[x]));

		It should_have_all_of_the_message_provided = () =>
			built.Messages.SequenceEqual(messages).ShouldBeTrue();

		It should_mark_the_message_as_non_expiring = () =>
			built.Expiration.ShouldEqual(DateTime.MaxValue);

		It should_mark_the_message_as_durable = () =>
			built.Persistent.ShouldBeTrue();

		static readonly Guid correlationId = Guid.NewGuid();
		static readonly Uri returnAddress = new Uri("direct://default/return-address/");
		static readonly IDictionary<string, string> headers = new Dictionary<string, string>();
		static readonly object[] messages = new object[] { 1, "2", 3.0, 4.0M, -5, 0x6 };
	}

	[Subject(typeof(DefaultChannelMessageBuilder))]
	public class when_no_return_address_is_specified : with_a_message_builder
	{
		Because of = () =>
			built = builder.Build(Guid.Empty, null, null, new object[0]);

		It should_not_have_a_return_address_on_the_message = () =>
			built.ReturnAddress.ShouldBeNull();
	}

	[Subject(typeof(DefaultChannelMessageBuilder))]
	public class when_a_channel_message_is_built_using_a_null_set_of_headers : with_a_message_builder
	{
		Because of = () =>
			built = builder.Build(Guid.Empty, null, null, new object[0]);

		It should_build_a_message_with_an_empty_set_of_headers = () =>
			built.Headers.Count.ShouldEqual(0);
	}

	[Subject(typeof(DefaultChannelMessageBuilder))]
	public class when_a_channel_message_is_built_using_a_null_set_of_messages : with_a_message_builder
	{
		Because of = () =>
			built = builder.Build(Guid.Empty, null, null, null);

		It should_build_a_message_with_an_empty_set_of_messages = () =>
			built.Messages.Count.ShouldEqual(0);
	}

	[Subject(typeof(DefaultChannelMessageBuilder))]
	public class when_the_primary_logical_message_type_has_been_marked_as_transient : with_a_message_builder
	{
		Establish context = () =>
			builder.MarkAsTransient(messages[0].GetType());

		Because of = () =>
			built = builder.Build(Guid.Empty, null, null, messages);

		It should_mark_the_channel_message_as_nonpersistent = () =>
			built.Persistent.ShouldBeFalse();

		static readonly object[] messages = new object[] { string.Empty };
	}

	[Subject(typeof(DefaultChannelMessageBuilder))]
	public class when_the_primary_logical_message_type_has_been_marked_as_expiring : with_a_message_builder
	{
		Establish context = () =>
			builder.MarkAsExpiring(messages[0].GetType(), timeToLive);

		Because of = () =>
			built = builder.Build(Guid.Empty, null, null, messages);

		It should_correctly_set_the_channel_message_expiration = () =>
			built.Expiration.ShouldBeCloseTo(SystemTime.UtcNow + timeToLive, TimeSpan.FromSeconds(1));

		static readonly TimeSpan timeToLive = TimeSpan.FromDays(1);
		static readonly object[] messages = new object[] { string.Empty };
	}

	[Subject(typeof(DefaultChannelMessageBuilder))]
	public class when_marking_a_null_type_as_transient : with_a_message_builder
	{
		Because of = () =>
			Try(() => builder.MarkAsTransient(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultChannelMessageBuilder))]
	public class when_marking_a_null_type_as_expiring : with_a_message_builder
	{
		Because of = () =>
			Try(() => builder.MarkAsExpiring(null, TimeSpan.MaxValue));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultChannelMessageBuilder))]
	public class when_providing_a_non_positive_expiration_value : with_a_message_builder
	{
		Because of = () =>
			Try(() => builder.MarkAsExpiring(typeof(int), TimeSpan.Zero));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	public abstract class with_a_message_builder
	{
		Establish context = () =>
		{
			built = null;
			thrown = null;
			builder = new DefaultChannelMessageBuilder();
		};
		protected static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		protected static DefaultChannelMessageBuilder builder;
		protected static ChannelMessage built;
		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169