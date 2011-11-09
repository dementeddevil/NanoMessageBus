namespace NanoMessageBus.RabbitMQ
{
	using System;
	using System.IO;
	using System.Linq;
	using Serialization;

	public static class EnvelopeMessageExtensions
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
	}
}