namespace NanoMessageBus.Transports
{
	using System;
	using System.Collections.Generic;

	public class MessageBuilder
	{
		public virtual EnvelopeMessage BuildMessage(IDictionary<string, string> headers, params object[] messages)
		{
			if (messages == null || 0 == messages.Length)
				return null;

			var primaryMessageType = messages[0].GetType();
			return new EnvelopeMessage(
				Guid.NewGuid(),
				this.localAddress,
				this.GetTimeToLive(primaryMessageType),
				this.IsPersistent(primaryMessageType),
				headers,
				messages);
		}
		private TimeSpan GetTimeToLive(Type messageType)
		{
			TimeSpan ttl;
			return this.timeToLive.TryGetValue(messageType, out ttl) ? ttl : TimeSpan.MaxValue;
		}
		private bool IsPersistent(Type messageType)
		{
			return !this.transientMessages.Contains(messageType);
		}

		public virtual void RegisterMaximumMessageLifetime(Type messageType, TimeSpan ttl)
		{
			this.timeToLive[messageType] = ttl;
		}
		public virtual void RegisterTransientMessage(Type messageType)
		{
			this.transientMessages.Add(messageType);
		}

		public MessageBuilder(Uri localAddress)
			: this(null, null, localAddress)
		{
		}
		public MessageBuilder(
			IDictionary<Type, TimeSpan> timeToLive, ICollection<Type> transientMessages, Uri localAddress)
		{
			this.timeToLive = timeToLive ?? new Dictionary<Type, TimeSpan>();
			this.transientMessages = transientMessages ?? new HashSet<Type>();
			this.localAddress = localAddress;
		}

		private readonly IDictionary<Type, TimeSpan> timeToLive;
		private readonly ICollection<Type> transientMessages;
		private readonly Uri localAddress;
	}
}