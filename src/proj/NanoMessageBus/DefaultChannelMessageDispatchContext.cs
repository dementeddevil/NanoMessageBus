namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using Logging;

	public class DefaultChannelMessageDispatchContext : IDispatchContext
	{
		public int MessageCount
		{
			get { return 1; }
		}
		public int HeaderCount
		{
			get { return 0; }
		}
		public IDispatchContext WithMessage(object message)
		{
			throw new InvalidOperationException("The message collection cannot be modified.");
		}
		public IDispatchContext WithMessages(params object[] messages)
		{
			throw new InvalidOperationException("The message collection cannot be modified.");
		}
		public IDispatchContext WithCorrelationId(Guid correlationId)
		{
			throw new InvalidOperationException("A correlation identifier is already set.");
		}
		public IDispatchContext WithHeader(string key, string value = null)
		{
			throw new InvalidOperationException("The headers cannot be modified.");
		}
		public IDispatchContext WithHeaders(IDictionary<string, string> headers)
		{
			throw new InvalidOperationException("The headers cannot be modified.");
		}
		public IDispatchContext WithRecipient(Uri recipient)
		{
			throw new InvalidOperationException("The recipients cannot be modified.");
		}
		public IChannelTransaction Send()
		{
			this.ThrowWhenDispatched();
			this.dispatched = true;

			this.channel.Send(this.envelope);
			return this.channel.CurrentTransaction;
		}
		public IChannelTransaction Publish()
		{
			throw new InvalidOperationException("Only send can be invoked.");
		}
		public IChannelTransaction Reply()
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

		public DefaultChannelMessageDispatchContext(IMessagingChannel channel, ChannelEnvelope envelope)
		{
			if (channel == null)
				throw new ArgumentNullException("channel");

			if (envelope == null)
				throw new ArgumentNullException("envelope");

			this.channel = channel;
			this.envelope = envelope;
		}

		private static readonly ILog Log = LogFactory.Build(typeof(DefaultChannelMessageDispatchContext));
		private readonly IMessagingChannel channel;
		private readonly ChannelEnvelope envelope;
		private bool dispatched;
	}
}