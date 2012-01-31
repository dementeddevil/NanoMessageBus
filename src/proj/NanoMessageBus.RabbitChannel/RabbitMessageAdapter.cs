namespace NanoMessageBus.RabbitChannel
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
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
				Log.Error("Unable to deserialize message: {0}", e.Message);
				throw;
			}
			catch (DeadLetterException)
			{
				throw;
			}
			catch (Exception e)
			{
				Log.Error("General deserialize error for message: {0}", e.Message);
				throw new SerializationException(e.Message, e);
			}
		}
		protected virtual ChannelMessage Translate(BasicDeliverEventArgs message)
		{
			var properties = message.BasicProperties;
			var expiration = properties.Expiration.ToDateTime();
			expiration = expiration == DateTime.MinValue ? DateTime.MaxValue : expiration;

			if (expiration <= SystemTime.UtcNow)
				throw new DeadLetterException();

			var payload = this.configuration.Serializer.Deserialize<object[]>(
				message.Body, properties.ContentFormat(), properties.ContentEncoding);

			return new ChannelMessage(
				properties.MessageId.ToGuid(),
				properties.CorrelationId.ToGuid(),
				properties.ReplyTo.ToUri(),
				new Dictionary<string, string>(),
				payload)
			{
				Expiration = properties.Expiration.ToDateTime(),
				Persistent = properties.DeliveryMode == Persistent
			};
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
				Log.Error("Unable to serialize message {0}: {1}", message.MessageId, e.Message);
				throw;
			}
			catch (Exception e)
			{
				Log.Error("General serialization failure for message {0}: {1}", message.MessageId, e.Message);
				throw new SerializationException(e.Message, e);
			}
		}
		protected virtual BasicDeliverEventArgs Translate(ChannelMessage message, IBasicProperties properties)
		{
			var serializer = this.configuration.Serializer;

			properties.MessageId = message.MessageId.ToString();
			properties.CorrelationId = message.CorrelationId.ToString();
			properties.AppId = this.configuration.ApplicationId;
			properties.ContentEncoding = serializer.ContentEncoding ?? string.Empty;

			properties.ContentType = string.IsNullOrEmpty(serializer.ContentFormat)
				? ContentType : ContentType + "+" + serializer.ContentFormat;

			properties.SetPersistent(message.Persistent);

			var expiration = message.Expiration;
			properties.Expiration = (expiration == DateTime.MinValue || expiration == DateTime.MaxValue)
				? string.Empty : expiration.ToString(CultureInfo.InvariantCulture);

			if (message.ReturnAddress != null)
				properties.ReplyTo = message.ReturnAddress.ToString();

			var messages = (message.Messages ?? new object[0]).ToArray();
			var payload = serializer.Serialize(messages);

			properties.Headers = new Hashtable((IDictionary)message.Headers);
			properties.Type = messages[0].GetType().FullName;
			properties.Timestamp = new AmqpTimestamp(SystemTime.UtcNow.ToEpochTime());

			return new BasicDeliverEventArgs
			{
				Body = payload,
				RoutingKey = this.configuration.LookupRoutingKey(message),
				BasicProperties = properties
			};
		}

		public virtual void AppendException(BasicDeliverEventArgs message, Exception exception)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			if (exception == null)
				throw new ArgumentNullException("exception");

			this.AppendException(message, exception, 0);
		}
		protected virtual void AppendException(BasicDeliverEventArgs message, Exception exception, int depth)
		{
			if (exception == null)
				return;

			message.SetHeader(ExceptionHeaderFormat.FormatWith(depth, "type"), exception.GetType().ToString());
			message.SetHeader(ExceptionHeaderFormat.FormatWith(depth, "message"), exception.Message);
			message.SetHeader(ExceptionHeaderFormat.FormatWith(depth, "stacktrace"), exception.StackTrace ?? string.Empty);

			this.AppendException(message, exception.InnerException, depth + 1);
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
		private const string RabbitHeaderFormat = "x-rabbit-{0}";
		private const string ExceptionHeaderFormat = "x-exception{0}-{1}";
		private static readonly ILog Log = LogFactory.Build(typeof(RabbitMessageAdapter));
		private readonly RabbitChannelGroupConfiguration configuration;
	}
}