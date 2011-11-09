namespace NanoMessageBus.Transports
{
	using System;
	using System.Collections.Generic;

	public class MessageBuilder
	{
		public virtual EnvelopeMessage BuildMessage(params object[] messages)
		{
			if (messages == null || 0 == messages.Length)
				return null;

			var primaryMessageType = messages[0].GetType();
			var envelope = this.BuildMessage(primaryMessageType, messages);
			this.appender.AppendHeaders(envelope);
			return envelope;
		}
		private EnvelopeMessage BuildMessage(Type primary, ICollection<object> messages)
		{
			return new EnvelopeMessage(
				Guid.NewGuid(),
				Guid.Empty,
				this.localAddress,
				TimeSpan.MaxValue, // TODO: grab from DescriptionAttribute (careful of threading issues)
				true,
				null,
				messages);
		}

		public MessageBuilder(IAppendHeaders appender, Uri localAddress)
		{
			this.appender = appender ?? new NullHeaderAppender();
			this.localAddress = localAddress;
		}

		private readonly IAppendHeaders appender;
		private readonly Uri localAddress;
	}
}