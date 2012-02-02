namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using Logging;

	public class DefaultChannelMessageDispatchContext : IDispatchContext
	{
		public virtual int MessageCount
		{
			get { return this.dispatched ? 0 : 1; }
		}
		public virtual int HeaderCount
		{
			get { return 0; }
		}
		public virtual IDispatchContext WithMessage(object message)
		{
			throw new InvalidOperationException("The message collection cannot be modified.");
		}
		public virtual IDispatchContext WithMessages(params object[] messages)
		{
			throw new InvalidOperationException("The message collection cannot be modified.");
		}
		public virtual IDispatchContext WithCorrelationId(Guid correlationId)
		{
			throw new InvalidOperationException("A correlation identifier is already set.");
		}
		public virtual IDispatchContext WithHeader(string key, string value = null)
		{
			throw new InvalidOperationException("The headers cannot be modified.");
		}
		public virtual IDispatchContext WithHeaders(IDictionary<string, string> headers)
		{
			throw new InvalidOperationException("The headers cannot be modified.");
		}
		public virtual IDispatchContext WithRecipient(Uri recipient)
		{
			if (recipient == null)
				throw new ArgumentNullException("recipient");

			this.recipients.Add(recipient);
			return this;
		}
		public virtual IChannelTransaction Send()
		{
			this.ThrowWhenDispatched();
			this.dispatched = true;

			this.channel.Send(new ChannelEnvelope(this.channelMessage, this.recipients));
			return this.channel.CurrentTransaction;
		}
		public virtual IChannelTransaction Publish()
		{
			throw new InvalidOperationException("Only send can be invoked.");
		}
		public virtual IChannelTransaction Reply()
		{
			throw new InvalidOperationException("Only send can be invoked.");
		}

		protected virtual void ThrowWhenDispatched()
		{
			if (!this.dispatched)
				return;

			Log.Warn("The set of messages has already been dispatched.");
			throw new InvalidOperationException("The set of messages has already been dispatched.");
		}

		public DefaultChannelMessageDispatchContext(IMessagingChannel channel, ChannelMessage channelMessage)
		{
			if (channel == null)
				throw new ArgumentNullException("channel");

			if (channelMessage == null)
				throw new ArgumentNullException("channelMessage");

			this.channel = channel;
			this.channelMessage = channelMessage;
		}

		private static readonly ILog Log = LogFactory.Build(typeof(DefaultChannelMessageDispatchContext));
		private readonly ICollection<Uri> recipients = new LinkedList<Uri>();
		private readonly IMessagingChannel channel;
		private readonly ChannelMessage channelMessage;
		private bool dispatched;
	}
}