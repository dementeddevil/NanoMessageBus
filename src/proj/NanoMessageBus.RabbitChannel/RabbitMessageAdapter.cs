namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.Linq;
	using System.Runtime.Serialization;
	using System.Text;
	using Logging;
	using RabbitMQ.Client;
	using RabbitMQ.Client.Events;
	using Serialization;

	public class RabbitMessageAdapter
	{
		public virtual ChannelMessage Build(BasicDeliverEventArgs message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			try
			{
				var result = this.Translate(message);
				this.AppendHeaders(result, message.BasicProperties);
				return result;
			}
			catch (SerializationException e)
			{
				Log.Error("Unable to deserialize message '{0}'.".FormatWith(message.MessageId()), e);
				throw new PoisonMessageException(e.Message, e);
			}
			catch (DeadLetterException)
			{
				throw;
			}
			catch (Exception e)
			{
				Log.Error("General deserialize error for message '{0}'.".FormatWith(message.MessageId()), e);
				throw new PoisonMessageException(e.Message, e);
			}
		}
		protected virtual ChannelMessage Translate(BasicDeliverEventArgs message)
		{
			var properties = message.BasicProperties;

			var dispatched = properties.Timestamp.UnixTime.ToDateTime();
			var expiration = properties.Expiration.ToDateTime(dispatched);
			expiration = expiration == DateTime.MinValue ? DateTime.MaxValue : expiration;

			if (expiration <= SystemTime.UtcNow)
				throw new DeadLetterException(expiration);

			var payload = this.Deserialize(message.Body, properties.Type, properties.ContentFormat(), properties.ContentEncoding);

			return new ChannelMessage(
				properties.MessageId.ToGuid(),
				properties.CorrelationId.ToGuid(),
				properties.ReplyTo.ToUri(),
				new Dictionary<string, string>(),
				payload)
			{
				Dispatched = properties.Timestamp.UnixTime.ToDateTime(),
				Expiration = expiration,
				Persistent = properties.DeliveryMode == Persistent
			};
		}
		private IEnumerable<object> Deserialize(byte[] body, string type, string format, string encoding)
		{
			var parsedType = Type.GetType(type, false, true) ?? typeof(object);
			var deserialized = this.configuration.Serializer.Deserialize(body, parsedType, format, encoding);
			var collection = deserialized as object[];
			return collection ?? new[] { deserialized };
		}
		protected virtual void AppendHeaders(ChannelMessage message, IBasicProperties properties)
		{
			var headers = message.Headers;
			headers[RabbitHeaderFormat.FormatWith("appId")] = properties.AppId;
			headers[RabbitHeaderFormat.FormatWith("clusterId")] = properties.ClusterId;
			headers[RabbitHeaderFormat.FormatWith("userId")] = properties.UserId;
			headers[RabbitHeaderFormat.FormatWith("type")] = properties.Type;
			headers[RabbitHeaderFormat.FormatWith("priority")] = properties.Priority.ToString(CultureInfo.InvariantCulture);

			var encoding = Encoding.UTF8;
			foreach (var key in properties.Headers.Keys.Cast<string>())
			{
				var value = properties.Headers[key];
				if (value is int)
					headers[key] = ((int)value).ToString(CultureInfo.InvariantCulture);
				else
					headers[key] = encoding.GetString((byte[])value);
			}
		}

		public virtual BasicDeliverEventArgs Build(ChannelMessage message, IBasicProperties properties)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			if (properties == null)
				throw new ArgumentNullException("properties");

			try
			{
				return this.Translate(message, properties);
			}
			catch (SerializationException e)
			{
				Log.Error("Unable to serialize message '{0}'.".FormatWith(message.MessageId), e);
				throw new PoisonMessageException(e.Message, e);
			}
			catch (Exception e)
			{
				Log.Error("General serialization failure for message '{0}'.".FormatWith(message.MessageId), e);
				throw new PoisonMessageException(e.Message, e);
			}
		}
		protected virtual BasicDeliverEventArgs Translate(ChannelMessage message, IBasicProperties properties)
		{
			var serializer = this.configuration.Serializer;

			properties.MessageId = message.MessageId.ToNull() ?? string.Empty;
			properties.CorrelationId = message.CorrelationId.ToNull() ?? string.Empty;
			properties.AppId = this.configuration.ApplicationId;
			properties.ContentEncoding = serializer.ContentEncoding ?? string.Empty;

			properties.ContentType = string.IsNullOrEmpty(serializer.ContentFormat)
				? ContentType : ContentType + "+" + serializer.ContentFormat;

			properties.SetPersistent(message.Persistent);
			SetExpiration(properties, message);

			if (message.ReturnAddress != null)
				properties.ReplyTo = message.ReturnAddress.ToString();

			var messages = (message.Messages ?? new object[0]).ToArray();
			var payload = messages.Length > 1 ? serializer.Serialize(messages) : serializer.Serialize(messages[0]);
			properties.Headers = new Hashtable((IDictionary)message.Headers);

			var type = messages[0].GetType();
			properties.Type = MessageTypeFormat.FormatWith(type.FullName, type.Assembly.GetName().Name);
			properties.Timestamp = new AmqpTimestamp(SystemTime.UtcNow.ToEpochTime());

			return new BasicDeliverEventArgs
			{
				Body = payload,
				RoutingKey = this.configuration.LookupRoutingKey(message),
				BasicProperties = properties
			};
		}
		private static void SetExpiration(IBasicProperties properties, ChannelMessage message)
		{
			var expiration = message.Expiration;
			if (expiration <= DateTime.MinValue || expiration >= DateTime.MaxValue)
				return;

			var dispatch = message.Dispatched > DateTime.MinValue ? message.Dispatched : SystemTime.UtcNow;
			var ttl = (long)(expiration - dispatch).TotalMilliseconds;
			if (ttl > int.MaxValue)
				return;

			if (ttl < 0)
				ttl = 0;

			properties.Expiration = ttl.ToString(CultureInfo.InvariantCulture);
		}

		public virtual void AppendRetryAddress(BasicDeliverEventArgs message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			var address = RetryAddressValueFormat.FormatWith(this.configuration.InputQueue);
			message.BasicProperties.Headers[RabbitHeaderFormat.FormatWith(RetryAddressHeaderKey)] = address;

			Log.Verbose("Poison message source address is '{0}'", address);
		}

		public virtual void AppendException(BasicDeliverEventArgs message, Exception exception, int attempt)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			if (exception == null)
				throw new ArgumentNullException("exception");

			this.AppendException(message, exception, attempt, 0);
		}
		protected virtual void AppendException(BasicDeliverEventArgs message, Exception exception, int attempt, int depth)
		{
			if (!this.CanAppendException(message, exception, attempt, depth))
				return;

			message.SetHeader(ExceptionHeaderFormat.FormatWith(attempt, depth, "type"), exception.GetType().ToString());
			message.SetHeader(ExceptionHeaderFormat.FormatWith(attempt, depth, "message"), exception.Message);
			message.SetHeader(ExceptionHeaderFormat.FormatWith(attempt, depth, "stacktrace"), exception.StackTrace ?? string.Empty);
			this.AppendException(message, exception.InnerException, attempt, depth + 1);

			if (depth > 0)
				return;

			var process = Process.GetCurrentProcess();
			message.SetHeader(ExceptionHeaderFormat.FormatWith(attempt, depth, "process-name"), process.ProcessName);
			message.SetHeader(ExceptionHeaderFormat.FormatWith(attempt, depth, "process-id"), process.Id.ToString(CultureInfo.InvariantCulture));
			message.SetHeader(ExceptionHeaderFormat.FormatWith(attempt, depth, "origin-host"), Environment.MachineName.ToLowerInvariant());
			message.SetHeader(ExceptionHeaderFormat.FormatWith(attempt, depth, "timestamp"), SystemTime.UtcNow.ToIsoString());
		}
		protected virtual bool CanAppendException(BasicDeliverEventArgs message, Exception exception, int attempt, int depth)
		{
			if (exception == null)
				return false;

			if (attempt == 0 || depth > 1)
				return true;

			var previousType = message.GetHeader(ExceptionHeaderFormat.FormatWith(attempt - 1, 0, "type")).AsString();
			var previousMessage = message.GetHeader(ExceptionHeaderFormat.FormatWith(attempt - 1, 0, "message")).AsString();
			var previousStackTrace = message.GetHeader(ExceptionHeaderFormat.FormatWith(attempt - 1, 0, "stacktrace")).AsString();

			return exception.GetType().ToString() != previousType
				|| exception.Message != previousMessage
				|| (exception.StackTrace ?? string.Empty) != previousStackTrace;
		}

		public RabbitMessageAdapter(RabbitChannelGroupConfiguration configuration) : this()
		{
			this.configuration = configuration;
		}
		protected RabbitMessageAdapter()
		{
		}

		private const byte Persistent = 2;
		private const string ContentType = "application/vnd.nmb.rabbit-msg";
		private const string MessageTypeFormat = "{0}, {1}";
		private const string RabbitHeaderFormat = "x-rabbit-{0}";
		private const string ExceptionHeaderFormat = "x-exception{0}.{1}-{2}";
		private const string RetryAddressHeaderKey = "retry-address";
		private const string RetryAddressValueFormat = "direct://default/{0}";
		private static readonly ILog Log = LogFactory.Build(typeof(RabbitMessageAdapter));
		private readonly RabbitChannelGroupConfiguration configuration;
	}
}