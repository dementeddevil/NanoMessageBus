#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.RabbitChannel
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
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
			thrown = Catch.Exception(() => adapter.Build(null));

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
				ContentType = "content type+" + DefaultContentFormat,
				ContentEncoding = "content encoding",
				CorrelationId = Guid.NewGuid().ToString(),
				DeliveryMode = 2, // persistent
				Expiration = DateTime.Parse("2150-01-02 03:04:05").ToString(),
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
				.Setup(x => x.Deserialize<object[]>(Moq.It.IsAny<Stream>(), DefaultContentFormat, message.BasicProperties.ContentEncoding))
				.Returns(deserialized);
		};

		Because of = () =>
			result = adapter.Build(message);

		It should_deserialize_the_payload = () =>
			mockSerializer.Verify(x =>
				x.Deserialize<object[]>(Moq.It.IsAny<Stream>(), DefaultContentFormat, message.BasicProperties.ContentEncoding),
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
	public class when_the_wire_message_is_expired : using_a_message_adapter
	{
		Establish context = () =>
		{
			message = EmptyMessage();
			message.BasicProperties.Expiration = SystemTime.UtcNow.AddMinutes(-1).ToString();
		};

		Because of = () =>
			result = adapter.Build(message);

		It should_build_the_resulting_message = () =>
			result.ShouldBeNull();

		It should_not_invoke_the_serializer = () =>
			mockSerializer.Verify(
				x => x.Deserialize<object[]>(Moq.It.IsAny<Stream>(), Moq.It.IsAny<string>(), Moq.It.IsAny<string>()),
				Times.Never());

		static BasicDeliverEventArgs message;
		static ChannelMessage result;
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
	public class when_unable_to_deserialize_a_wire_message_body : using_a_message_adapter
	{
		Establish context = () => mockSerializer
			.Setup(x => x.Deserialize<object[]>(Moq.It.IsAny<Stream>(), Moq.It.IsAny<string>(), Moq.It.IsAny<string>()))
			.Throws(new SerializationException());

		Because of = () =>
			thrown = Catch.Exception(() => adapter.Build(EmptyMessage()));

		It should_throw_an_exception = () =>
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
			thrown = Catch.Exception(() => adapter.Build(null, new BasicProperties()));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(RabbitMessageAdapter))]
	public class when_no_basic_properties_are_supplied_to_build_a_wire_message : using_a_message_adapter
	{
		Because of = () =>
			thrown = Catch.Exception(() => adapter.Build(new Mock<ChannelMessage>().Object, null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(RabbitMessageAdapter))]
	public class when_translating_a_ChannelMessage_into_a_wire_message : using_a_message_adapter
	{
		Establish context = () =>
		{
			message = new ChannelMessage(
				Guid.NewGuid(),
				Guid.NewGuid(),
				new Uri("direct://MyExchange/RoutingKey"),
				new Dictionary<string, string>(),
				new object[] { "1", 2, 3.0, 4.0M });
			message.Headers["a"] = "b";
			message.Headers["c"] = "d";
			message.Persistent = true;
			message.Expiration = DateTime.Parse("2010-07-01 12:34:56");

			mockSerializer.Setup(x => x.ContentEncoding).Returns(DefaultContentEncoding);
			mockConfiguration.Setup(x => x.ApplicationId).Returns(DefaultAppId);
			mockConfiguration.Setup(x => x.LookupRoutingKey(message)).Returns(DefaultRoutingKey);
			mockSerializer
				.Setup(x => x.Serialize(Moq.It.IsAny<Stream>(), message.Messages))
				.Callback<Stream, object>((stream, graph) => stream.Write(body, 0, body.Length));
		};

		Because of = () =>
			result = adapter.Build(message, new BasicProperties());

		It should_serialize_the_ChannelMessage_payload = () =>
			mockSerializer.Verify(x => x.Serialize(Moq.It.IsAny<Stream>(), message.Messages), Times.Once());

		It should_return_a_wire_message = () =>
			result.ShouldNotBeNull();

		It should_populate_the_wire_message_with_the_correct_routing_key = () =>
			result.RoutingKey.ShouldEqual(DefaultRoutingKey);

		It should_populate_the_wire_message_with_the_correct_message_id = () =>
			result.BasicProperties.MessageId.ToGuid().ShouldEqual(message.MessageId);

		It should_populate_the_wire_message_with_the_correct_correlation_id = () =>
			result.BasicProperties.CorrelationId.ToGuid().ShouldEqual(message.CorrelationId);

		It should_populate_the_wire_message_with_the_correct_persistence_mode = () =>
			result.BasicProperties.DeliveryMode.ShouldEqual((byte)2);

		It should_populate_the_wire_message_with_the_correct_return_address = () =>
			result.BasicProperties.ReplyTo.ShouldEqual(message.ReturnAddress.ToString());

		It should_populate_the_wire_message_with_the_correct_expiration_time = () =>
			result.BasicProperties.Expiration.ShouldEqual(message.Expiration.ToString());

		It should_populate_the_wire_message_with_the_correct_message_type = () =>
			result.BasicProperties.Type.ShouldEqual(message.Messages.First().GetType().FullName);

		It should_populate_the_wire_message_with_the_correct_content_encoding = () =>
			result.BasicProperties.ContentEncoding.ShouldEqual(DefaultContentEncoding);

		It should_populate_the_wire_message_with_the_correct_content_type = () =>
			result.BasicProperties.ContentType.ShouldEqual("application/vnd.nmb.rabbit-msg+" + DefaultContentFormat);

		It should_populate_the_wire_message_with_the_correct_app_id = () =>
			result.BasicProperties.AppId.ShouldEqual(DefaultAppId);

		It should_populate_the_wire_message_with_the_correct_cluster_id = () =>
			result.BasicProperties.ClusterId.ShouldBeEmpty();

		It should_populate_the_wire_message_with_the_correct_user_id = () =>
			result.BasicProperties.UserId.ShouldBeEmpty();

		It should_populate_the_wire_message_with_the_correct_body = () =>
			result.Body.SequenceEqual(body).ShouldBeTrue();

		It should_populate_the_wire_message_with_the_correct_headers = () =>
		{
			foreach (var item in message.Headers)
				result.BasicProperties.Headers[item.Key].ShouldEqual(item.Value);
		};

		const string DefaultContentEncoding = "some crazy encoding scheme";
		const string DefaultAppId = "my producer application";
		const string DefaultRoutingKey = "configured routing key";
		static readonly byte[] body = new byte[] { 1, 2, 3, 4, 5, 6 };
		static ChannelMessage message;
		static BasicDeliverEventArgs result;
	}

	[Subject(typeof(RabbitMessageAdapter))]
	public class when_unable_to_serialize_a_ChannelMessage_payload : using_a_message_adapter
	{
		Establish context = () => mockSerializer
			.Setup(x => x.Serialize(Moq.It.IsAny<Stream>(), Moq.It.IsAny<object>()))
			.Throws(new SerializationException());

		Because of = () =>
			thrown = Catch.Exception(() => adapter.Build(new Mock<ChannelMessage>().Object, new BasicProperties()));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<SerializationException>();
	}

	[Subject(typeof(RabbitMessageAdapter))]
	public class when_a_malformd_ChannelMessage_payload_is_provided : using_a_message_adapter
	{
		Because of = () =>
			thrown = Catch.Exception(() => adapter.Build(new Mock<ChannelMessage>().Object, new BasicProperties()));

		It should_wrap_the_exception_inside_of_a_SerializationException = () =>
			thrown.ShouldBeOfType<SerializationException>();
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
			thrown = Catch.Exception(() => adapter.AppendException(EmptyMessage(), null));

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
			mockSerializer.Setup(x => x.ContentFormat).Returns(DefaultContentFormat);

			mockConfiguration = new Mock<RabbitChannelGroupConfiguration>();
			mockConfiguration.Setup(x => x.Serializer).Returns(mockSerializer.Object);
			adapter = new RabbitMessageAdapter(mockConfiguration.Object);
		};

		protected static BasicDeliverEventArgs EmptyMessage()
		{
			return new BasicDeliverEventArgs
			{
				Body = new byte[0],
				BasicProperties = new BasicProperties { Headers = new Hashtable() }
			};
		}

		protected const string DefaultContentFormat = "json";
		protected static Mock<RabbitChannelGroupConfiguration> mockConfiguration;
		protected static Mock<ISerializer> mockSerializer;
		protected static RabbitMessageAdapter adapter;
		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169