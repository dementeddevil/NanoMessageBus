namespace NanoMessageBus.RabbitMQ
{
	using System;
	using System.IO;
	using System.Linq;
	using Serialization;

	internal static class EnvelopeMessageExtensions
	{
		public static byte[] Serialize(this EnvelopeMessage message, ISerializer serializer)
		{
			if (message == null)
				return new byte[0];

			using (var stream = new MemoryStream())
			{
				serializer.Serialize(stream, message.LogicalMessages);
				return stream.ToArray();
			}
		}

		public static string MessageType(this EnvelopeMessage message)
		{
			if (message == null || message.LogicalMessages == null || message.LogicalMessages.Count == 0)
				return string.Empty;

			// TODO: read TTL attribute from message and use it if any exists.
			return message.LogicalMessages.First().GetType().FullName;
		}

		public static DateTime Expiration(this EnvelopeMessage message)
		{
			if (message.TimeToLive == TimeSpan.MaxValue)
				return DateTime.MaxValue;

			return SystemTime.UtcNow + message.TimeToLive;
		}

		public static string RoutingKey(this EnvelopeMessage message)
		{
			if (message == null || message.LogicalMessages == null || message.LogicalMessages.Count == 0)
				return string.Empty;

			// TODO: read routing key attribute from message and use it if any exists.
			var name = message.LogicalMessages.First().GetType().FullName ?? string.Empty;
			return name.ToLowerInvariant();
		}

		public static int FailureCount(this EnvelopeMessage message)
		{
			if (message == null || message.Headers == null)
				return 0;

			string value;
			return message.Headers.TryGetValue(RabbitKeys.FailureCount, out value) ? value.ToInt() : 0;
		}
		private static int ToInt(this string value)
		{
			int parsed;
			return int.TryParse(value ?? string.Empty, out parsed) ? parsed : 0;
		}
	}
}