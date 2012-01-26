namespace NanoMessageBus.RabbitChannel
{
	using System;
	using System.Collections;
	using RabbitMQ.Client;
	using RabbitMQ.Client.Events;
	using RabbitMQ.Client.Framing.v0_9;

	public static class ExtensionMethods
	{
		public static PublicationAddress ToPublicationAddress(this string queueName)
		{
			return new PublicationAddress(ExchangeType.Direct, string.Empty, queueName);
		}
		public static PublicationAddress ToExchangeAddress(this string exchangeName, string exchangeType = ExchangeType.Fanout)
		{
			if (string.IsNullOrEmpty(exchangeName))
				return null;

			return new PublicationAddress(exchangeType, exchangeName, string.Empty);
		}
		public static PublicationAddress ToPublicationAddress(this Uri uri, RabbitChannelGroupConfiguration config)
		{
			if (uri == ChannelEnvelope.LoopbackAddress)
				return new PublicationAddress(ExchangeType.Direct, string.Empty, config.InputQueue);

			if (uri == ChannelEnvelope.DeadLetterAddress)
				return config.DeadLetterExchange;

			var address = PublicationAddress.Parse(uri.ToString());
			return address.ExchangeName.AsLower() == "default"
				? new PublicationAddress(ExchangeType.Direct, string.Empty, address.RoutingKey) : address;
		}

		public static string AsLower(this string value)
		{
			return (value ?? string.Empty).ToLowerInvariant();
		}

		public static int GetAttemptCount(this BasicDeliverEventArgs message)
		{
			message.EnsureMessage();

			if (message.BasicProperties.Headers.Contains(AttemptCountHeader))
				return (int)message.BasicProperties.Headers[AttemptCountHeader];

			return 0;
		}
		public static void SetAttemptCount(this BasicDeliverEventArgs message, int count)
		{
			message.EnsureMessage();
			message.SetHeader(AttemptCountHeader, count);
		}

		public static object GetHeader(this BasicDeliverEventArgs message, string key)
		{
			message.EnsureMessage();
			return message.BasicProperties.Headers.Contains(key) ? message.BasicProperties.Headers[key] : null;
		}
		public static void SetHeader<T>(this BasicDeliverEventArgs message, string key, T value)
		{
			message.EnsureMessage();

			if (Equals(value, default(T)))
				message.BasicProperties.Headers.Remove(key);
			else
				message.BasicProperties.Headers[key] = value;
		}

		private static void EnsureMessage(this BasicDeliverEventArgs message)
		{
			message.BasicProperties = message.BasicProperties ?? new BasicProperties();
			message.BasicProperties.Headers = message.BasicProperties.Headers ?? new Hashtable();
		}

		public static Guid ToGuid(this string value)
		{
			Guid parsed;
			Guid.TryParse(value ?? string.Empty, out parsed);
			return parsed;
		}
		public static DateTime ToDateTime(this string value)
		{
			DateTime parsed;
			DateTime.TryParse(value, out parsed);
			return parsed;
		}
		public static Uri ToUri(this string value)
		{
			Uri parsed;
			Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out parsed);
			return parsed;
		}

		public static string ContentFormat(this IBasicProperties properties)
		{
			var contentType = properties.ContentType ?? string.Empty;
			var formatIndex = contentType.LastIndexOf("+", StringComparison.Ordinal);
			return formatIndex == -1 ? string.Empty : contentType.Substring(formatIndex + 1);
		}

		public static string MessageId(this BasicDeliverEventArgs message)
		{
			if (message == null || message.BasicProperties == null)
				return null;

			return message.BasicProperties.MessageId;
		}

		private const string AttemptCountHeader = "x-retry-count";
	}
}