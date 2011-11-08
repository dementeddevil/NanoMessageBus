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
			// TODO:  lock and inspect DescriptionAttribute of primary message (concurrent dictionary)
			return new EnvelopeMessage(
				Guid.NewGuid(),
				this.localAddress,
				TimeSpan.MaxValue,
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