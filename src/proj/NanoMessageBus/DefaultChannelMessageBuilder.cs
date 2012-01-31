namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class DefaultChannelMessageBuilder : IChannelMessageBuilder
	{
		public virtual void MarkAsTransient(Type messageType)
		{
			if (messageType == null)
				throw new ArgumentNullException("messageType");

			this.transient.Add(messageType);
		}
		public virtual void MarkAsExpiring(Type messageType, TimeSpan timeToLive)
		{
			if (messageType == null)
				throw new ArgumentNullException("messageType");

			if (timeToLive <= TimeSpan.Zero)
				throw new ArgumentException("The value must be positive", "timeToLive");

			this.expirations[messageType] = timeToLive;
		}

		public virtual ChannelMessage Build(
			Guid correlationId,
			Uri returnAddress,
			IDictionary<string, string> headers,
			object[] messages)
		{
			var message = new ChannelMessage(Guid.NewGuid(), correlationId, returnAddress, headers, messages)
			{
				Expiration = DateTime.MaxValue,
				Persistent = true
			};

			var primaryType = message.Messages.Count > 0 ? message.Messages.First().GetType() : null;
			if (primaryType == null)
				return message;

			message.Persistent = !this.transient.Contains(primaryType);

			TimeSpan timeToLive;
			if (this.expirations.TryGetValue(primaryType, out timeToLive))
				message.Expiration = SystemTime.UtcNow + timeToLive;

			return message;
		}

		private readonly IDictionary<Type, TimeSpan> expirations = new Dictionary<Type, TimeSpan>();
		private readonly ICollection<Type> transient = new HashSet<Type>();
	}
}