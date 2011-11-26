namespace NanoMessageBus.RabbitChannel
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.Serialization;
	using System.Text;
	using RabbitMQ.Client;
	using RabbitMQ.Client.Events;
	using RabbitMQ.Client.Framing.v0_9_1;
	using Serialization;

	public class RabbitMessageAdapter
	{
		public virtual ChannelMessage Build(BasicDeliverEventArgs message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			// we lock twice because we don't want to block during deserialization
			var translated = this.TryFromCache(message);
			if (translated != null)
				return translated;

			translated = this.TryTranslate(message);

			lock (this.cache)
				return this.cache[message] = translated; // can potentially be overwritten, but that's okay
		}
		protected virtual ChannelMessage TryFromCache(BasicDeliverEventArgs message)
		{
			lock (this.cache)
			{
				ChannelMessage cached;
				this.cache.TryGetValue(message, out cached);
				return cached;
			}
		}
		protected virtual ChannelMessage TryTranslate(BasicDeliverEventArgs message)
		{
			try
			{
				var result = this.Translate(message);
				this.AppendHeaders(result, message.BasicProperties);
				return result;
			}
			catch (SerializationException)
			{
				throw;
			}
			catch (Exception e)
			{
				throw new SerializationException(e.Message, e);
			}
		}
		protected virtual ChannelMessage Translate(BasicDeliverEventArgs message)
		{
			var properties = message.BasicProperties;
			var contentFormat = properties.ContentFormat();
			var expiration = properties.Expiration.ToDateTime();
			expiration = expiration == DateTime.MinValue ? DateTime.MaxValue : expiration;

			object[] payload = null;
			if (expiration >= SystemTime.UtcNow)
				payload = this.serializer.Deserialize<object[]>(
					message.Body, contentFormat, properties.ContentEncoding);

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
			headers[RabbitHeaderFormat.FormatWith("priority")] = properties.Priority.ToString();

			var encoding = Encoding.UTF8;
			foreach (var key in properties.Headers.Keys.Cast<string>())
				headers[key] = encoding.GetString((byte[])properties.Headers[key]);
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
			catch (SerializationException)
			{
				throw;
			}
			catch (Exception e)
			{
				throw new SerializationException(e.Message, e);
			}
		}
		protected virtual BasicDeliverEventArgs Translate(ChannelMessage message, IBasicProperties properties)
		{
			properties.MessageId = message.MessageId.ToString();
			properties.CorrelationId = message.CorrelationId.ToString();
			properties.AppId = this.configuration.ApplicationId;
			properties.ContentEncoding = this.serializer.ContentEncoding;

			properties.ContentType = string.IsNullOrEmpty(this.serializer.ContentFormat)
				? ContentType : ContentType + "+" + this.serializer.ContentFormat;

			properties.SetPersistent(message.Persistent);
			properties.Expiration = message.Expiration == DateTime.MinValue ? null : message.Expiration.ToString();

			if (message.ReturnAddress != null)
				properties.ReplyTo = message.ReturnAddress.ToString();

			properties.Headers = new Hashtable((IDictionary)message.Headers);
			properties.Type = message.Messages.First().GetType().FullName;
			properties.Timestamp = new AmqpTimestamp(SystemTime.UtcNow.ToEpochTime());

			return new BasicDeliverEventArgs
			{
				Body = this.serializer.Serialize(message.Messages),
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

			message.SetHeader(ExceptionHeaderFormat.FormatWith(depth, "type"), exception.GetType());
			message.SetHeader(ExceptionHeaderFormat.FormatWith(depth, "message"), exception.Message);
			message.SetHeader(ExceptionHeaderFormat.FormatWith(depth, "stacktrace"), exception.StackTrace ?? string.Empty);

			this.AppendException(message, exception.InnerException, depth + 1);
		}

		public virtual bool PurgeFromCache(BasicDeliverEventArgs message)
		{
			lock (this.cache)
				return this.cache.Remove(message);
		}

		public RabbitMessageAdapter(RabbitChannelGroupConfiguration configuration) : this()
		{
			this.configuration = configuration;
			this.serializer = configuration.Serializer;
		}
		protected RabbitMessageAdapter()
		{
		}

		private const byte Persistent = 2;
		private const string ContentType = "application/vnd.nmb.rabbit-msg";
		private const string RabbitHeaderFormat = "x-rabbit-{0}";
		private const string ExceptionHeaderFormat = "x-exception{0}-{1}";
		private readonly IDictionary<BasicDeliverEventArgs, ChannelMessage> cache =
			new Dictionary<BasicDeliverEventArgs, ChannelMessage>();
		private readonly RabbitChannelGroupConfiguration configuration;
		private readonly ISerializer serializer;
	}
}