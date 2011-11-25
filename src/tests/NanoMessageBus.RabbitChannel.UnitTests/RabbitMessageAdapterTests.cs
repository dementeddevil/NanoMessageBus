#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.RabbitChannel
{
	using System;
	using System.Collections;
	using System.IO;
	using System.Runtime.Serialization;
	using System.Text;
	using Machine.Specifications;
	using Moq;
	using RabbitMQ.Client;
	using RabbitMQ.Client.Events;
	using RabbitMQ.Client.Framing.v0_9_1;
	using Serialization;
	using It = Machine.Specifications.It;

	[Subject(typeof(RabbitMessageAdapter))]
	public class when_no_message_is_supplied_to_build_a_ChannelMessage_from_a_wire_message : using_a_message_adapter
	{
		Because of = () =>
			thrown = Catch.Exception(() => adapter.Build((BasicDeliverEventArgs)null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(RabbitMessageAdapter))]
	public class when_building_a_ChannelMessage : using_a_message_adapter
	{
		Establish context = () =>
		{
			message.Body = new byte[] { 1, 2, 3, 4 };
			message.BasicProperties = new BasicProperties
			{
				AppId = "appId",
				ClusterId = "clusterId",
				ContentType = "content type",
				ContentEncoding = "content encoding",
				CorrelationId = Guid.NewGuid().ToString(),
				DeliveryMode = 2, // persistent
				Expiration = DateTime.Parse("2000-01-02 03:04:05").ToString(),
				Headers = new Hashtable(),
				MessageId = Guid.NewGuid().ToString(),
				Type = "message type",
				UserId = "userId",
				Priority = 5,
				ReplyTo = "rabbitmq://localhost/ReplyTo",
				Timestamp = new AmqpTimestamp(0)
			};

			message.BasicProperties.Headers["MyHeader"] = Encoding.UTF8.GetBytes("MyValue");

			mockSerializer
				.Setup(x => x.Deserialize<object[]>(Moq.It.IsAny<Stream>(), message.BasicProperties.ContentEncoding))
				.Returns(deserialized);
		};

		Because of = () =>
			result = adapter.Build(message);

		It should_deserialize_the_payload = () =>
			mockSerializer.Verify(x =>
				x.Deserialize<object[]>(Moq.It.IsAny<Stream>(), message.BasicProperties.ContentEncoding),
				Times.Once());

		It should_return_a_ChannelMessage = () =>
			result.ShouldNotBeNull();

		It should_populate_the_ChannelMessage_with_the_correct_message_id = () =>
			result.MessageId.ShouldEqual(message.BasicProperties.MessageId.ToGuid());

		It should_populate_the_ChannelMessage_with_the_correct_correlation_id = () =>
			result.CorrelationId.ShouldEqual(message.BasicProperties.CorrelationId.ToGuid());

		It should_populate_the_ChannelMessage_with_the_correct_expiration = () =>
			result.Expiration.ShouldEqual(DateTime.Parse(message.BasicProperties.Expiration));

		It should_populate_the_ChannelMessage_with_the_correct_return_address = () =>
			result.ReturnAddress.ShouldEqual(message.BasicProperties.ReplyTo.ToUri());

		It should_populate_the_ChannelMessage_with_the_correct_persistence_value = () =>
			result.Persistent.ShouldBeTrue();

		It should_populate_the_ChannelMessage_with_the_correct_payload = () =>
			result.Messages.ShouldEqual(deserialized);

		It should_populate_the_ChannelMessage_header_with_the_AppId = () =>
			result.Headers["x-rabbit-appId"].ShouldEqual(message.BasicProperties.AppId);

		It should_populate_the_ChannelMessage_header_with_the_ClusterId = () =>
			result.Headers["x-rabbit-clusterId"].ShouldEqual(message.BasicProperties.ClusterId);

		It should_populate_the_ChannelMessage_header_with_the_UserId = () =>
			result.Headers["x-rabbit-userId"].ShouldEqual(message.BasicProperties.UserId);

		It should_populate_the_ChannelMessage_header_with_the_MessageType = () =>
			result.Headers["x-rabbit-type"].ShouldEqual(message.BasicProperties.Type);

		It should_populate_the_ChannelMessage_header_with_the_Priority = () =>
			result.Headers["x-rabbit-priority"].ShouldEqual(message.BasicProperties.Priority.ToString());

		It should_populate_the_ChannelMessage_headers_with_all_the_headers_from_the_wire_message = () =>
		{
			var headers = (Hashtable)message.BasicProperties.Headers;
			foreach (string key in headers.Keys)
				result.Headers[key].ShouldEqual(Encoding.UTF8.GetString((byte[])headers[key]));
		};

		It should_add_the_resulting_ChannelMessage_to_be_tracked = () =>
			adapter.PurgeFromCache(message).ShouldBeTrue();

		static ChannelMessage result;
		static readonly object[] deserialized = new object[] { 1, 2, 3.0, 4.0M };
		static readonly BasicDeliverEventArgs message = EmptyMessage();
	}

	[Subject(typeof(RabbitMessageAdapter))]
	public class building_a_ChannelMessage_from_a_cached_wire_message : using_a_message_adapter
	{
		Establish context = () =>
			first = adapter.Build(message);

		Because of = () =>
			second = adapter.Build(message);

		It should_return_a_reference_to_the_previously_built_ChannelMessage = () =>
			ReferenceEquals(first, second).ShouldBeTrue();

		static readonly BasicDeliverEventArgs message = EmptyMessage();
		static ChannelMessage first;
		static ChannelMessage second;
	}

	[Subject(typeof(RabbitMessageAdapter))]
	public class when_deserializing_a_wire_message_throws_any_exception : using_a_message_adapter
	{
		Establish context = () => mockSerializer
			.Setup(x => x.Deserialize<object[]>(Moq.It.IsAny<Stream>(), Moq.It.IsAny<string>()))
			.Throws(new Exception());

		Because of = () =>
			thrown = Catch.Exception(() => adapter.Build(EmptyMessage()));

		It should_wrap_the_exception_inside_of_a_SerializationException = () =>
			thrown.ShouldBeOfType<SerializationException>();
	}

	[Subject(typeof(RabbitMessageAdapter))]
	public class when_the_wire_message_is_malformed : using_a_message_adapter
	{
		Because of = () =>
			thrown = Catch.Exception(() => adapter.Build(new BasicDeliverEventArgs()));

		It should_wrap_the_exception_inside_of_a_SerializationException = () =>
			thrown.ShouldBeOfType<SerializationException>();
	}

	[Subject(typeof(RabbitMessageAdapter))]
	public class when_no_message_is_supplied_to_build_a_wire_message_from_a_ChannelMessage : using_a_message_adapter
	{
		Because of = () =>
			thrown = Catch.Exception(() => adapter.Build((ChannelMessage)null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(RabbitMessageAdapter))]
	public class when_translating_a_ChannelMessage_into_a_wire_message : using_a_message_adapter
	{
		It should_serialize_the_ChannelMessage_payload;
		It should_return_a_wire_message;
		It should_populate_the_wire_message_with_the_correct_routing_key;
		It should_populate_the_wire_message_with_the_correct_message_id;
		It should_populate_the_wire_message_with_the_correct_correlation_id;
		It should_populate_the_wire_message_with_the_correct_persistence_mode;
		It should_populate_the_wire_message_with_the_correct_return_address;
		It should_populate_the_wire_message_with_the_correct_creation_time;
		It should_populate_the_wire_message_with_the_correct_expiration_time;
		It should_populate_the_wire_message_with_the_correct_message_type;
		It should_populate_the_wire_message_with_the_correct_content_encoding;
		It should_populate_the_wire_message_with_the_correct_content_type;
		It should_populate_the_wire_message_with_the_correct_app_id;
		It should_populate_the_wire_message_with_the_correct_cluster_id;
		It should_populate_the_wire_message_with_the_correct_user_id;
		It should_populate_the_wire_message_with_the_correct_body;
		It should_populate_the_wire_message_with_the_correct_headers;
	}

	[Subject(typeof(RabbitMessageAdapter))]
	public class when_no_message_is_supplied_to_which_an_exception_should_be_appended : using_a_message_adapter
	{
		Because of = () =>
			thrown = Catch.Exception(() => adapter.AppendException(null, new Exception()));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(RabbitMessageAdapter))]
	public class when_no_exception_is_supplied_when_appending_an_exception : using_a_message_adapter
	{
		Because of = () =>
			thrown = Catch.Exception(() => adapter.AppendException(null, new Exception()));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(RabbitMessageAdapter))]
	public class when_adding_an_exception_to_the_wire_message : using_a_message_adapter
	{
		Because of = () =>
			adapter.AppendException(message, simple);

		It should_append_the_exception_message = () =>
			message.GetHeader("x-exception0-message").ShouldNotBeNull();

		It should_append_the_exception_type = () =>
			message.GetHeader("x-exception0-type").ShouldNotBeNull();

		It should_append_the_exception_stack_trace = () =>
			message.GetHeader("x-exception0-stacktrace").ShouldNotBeNull();

		static readonly BasicDeliverEventArgs message = EmptyMessage();
		static readonly Exception simple = new Exception();
	}

	[Subject(typeof(RabbitMessageAdapter))]
	public class when_adding_a_nested_exception_to_the_wire_message : using_a_message_adapter
	{
		Because of = () =>
			adapter.AppendException(message, nested);

		It should_append_the_inner_exception_message = () =>
			message.GetHeader("x-exception1-message").ShouldNotBeNull();

		It should_append_the_inner_exception_type = () =>
			message.GetHeader("x-exception1-type").ShouldNotBeNull();

		It should_append_the_inner_exception_stack_trace = () =>
			message.GetHeader("x-exception1-stacktrace").ShouldNotBeNull();

		static readonly BasicDeliverEventArgs message = EmptyMessage();
		static readonly Exception nested = new Exception("outer", new Exception("inner"));
	}

	[Subject(typeof(RabbitMessageAdapter))]
	public class when_adding_a_multi_layer_exception_to_the_wire_message : using_a_message_adapter
	{
		Because of = () =>
			adapter.AppendException(message, multiple);

		It should_append_each_layer_of_the_exception_message = () =>
			message.GetHeader("x-exception2-message").ShouldNotBeNull();

		It should_append_each_layer_of_the_exception_type = () =>
			message.GetHeader("x-exception2-type").ShouldNotBeNull();

		It should_append_each_layer_of_theexception_stack_trace = () =>
			message.GetHeader("x-exception2-stacktrace").ShouldNotBeNull();

		static readonly BasicDeliverEventArgs message = EmptyMessage();
		static readonly Exception multiple = new Exception("0", new Exception("1", new Exception("2")));
	}

	[Subject(typeof(RabbitMessageAdapter))]
	public class when_no_message_is_supplied_when_purging_the_message_from_the_cache : using_a_message_adapter
	{
		Because of = () =>
			thrown = Catch.Exception(() => adapter.AppendException(null, new Exception()));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(RabbitMessageAdapter))]
	public class when_purging_a_message_which_has_not_been_tracked : using_a_message_adapter
	{
		Because of = () =>
			cached = adapter.PurgeFromCache(EmptyMessage());

		It should_indicate_that_the_message_was_not_found_in_the_cache = () =>
			cached.ShouldBeFalse();

		static bool cached;
	}

	[Subject(typeof(RabbitMessageAdapter))]
	public class when_purging_a_message_which_been_tracked : using_a_message_adapter
	{
		Establish context = () =>
			adapter.Build(message);

		Because of = () =>
			cached = adapter.PurgeFromCache(message);

		It should_indicate_that_the_message_was_removed_from_cached = () =>
			cached.ShouldBeTrue();

		static readonly BasicDeliverEventArgs message = EmptyMessage();
		static bool cached;
	}

	[Subject(typeof(RabbitMessageAdapter))]
	public class when_purging_a_message_twice_from_the_adapter_cache : using_a_message_adapter
	{
		Establish context = () =>
			adapter.Build(message);

		Because of = () =>
		{
			first = adapter.PurgeFromCache(message);
			second = adapter.PurgeFromCache(message);
		};

		It should_first_indicate_that_the_message_was_removed_from_the_cache = () =>
			first.ShouldBeTrue();

		It should_then_indicate_that_the_message_was_not_found_in_the_cache = () =>
			second.ShouldBeFalse();

		static readonly BasicDeliverEventArgs message = EmptyMessage();
		static bool first;
		static bool second;
	}

	public abstract class using_a_message_adapter
	{
		Establish context = () =>
		{
			mockSerializer = new Mock<ISerializer>();
			adapter = new RabbitMessageAdapter(mockSerializer.Object);
		};

		protected static BasicDeliverEventArgs EmptyMessage()
		{
			return new BasicDeliverEventArgs
			{
				Body = new byte[0],
				BasicProperties = new BasicProperties { Headers = new Hashtable() }
			};
		}

		protected static Mock<ISerializer> mockSerializer;
		protected static RabbitMessageAdapter adapter;
		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169