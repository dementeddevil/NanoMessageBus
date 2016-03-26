namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using Logging;

	public class DefaultChannelMessageDispatchContext : IDispatchContext
	{
		public virtual int MessageCount => _dispatched ? 0 : 1;
	    public virtual int HeaderCount => 0;

	    public virtual IDispatchContext WithMessage(object message)
		{
			throw new NotSupportedException("The message collection cannot be modified.");
		}
		public virtual IDispatchContext WithMessages(params object[] messages)
		{
			throw new NotSupportedException("The message collection cannot be modified.");
		}
		public virtual IDispatchContext WithCorrelationId(Guid correlationId)
		{
			throw new NotSupportedException("A correlation identifier is already set.");
		}
		public virtual IDispatchContext WithHeader(string key, string value = null)
		{
			throw new NotSupportedException("The headers cannot be modified.");
		}
		public virtual IDispatchContext WithHeaders(IDictionary<string, string> headers)
		{
			throw new NotSupportedException("The headers cannot be modified.");
		}
		public virtual IDispatchContext WithRecipient(Uri recipient)
		{
			if (recipient == null)
			{
			    throw new ArgumentNullException(nameof(recipient));
			}

		    _recipients.Add(recipient);
			return this;
		}
		public virtual IDispatchContext WithState(object state)
		{
			throw new NotSupportedException("Envelope state cannot be specified.");
		}

		public virtual IChannelTransaction Send(params object[] messages)
		{
			ThrowWhenDispatched();
			_dispatched = true;

			_channel.SendAsync(new ChannelEnvelope(_channelMessage, _recipients, _channelMessage));
			return _channel.CurrentTransaction;
		}
		public virtual IChannelTransaction Publish(params object[] messages)
		{
			throw new NotSupportedException("Only send can be invoked.");
		}
		public virtual IChannelTransaction Reply(params object[] messages)
		{
			throw new NotSupportedException("Only send can be invoked.");
		}

		protected virtual void ThrowWhenDispatched()
		{
			if (!_dispatched)
			{
			    return;
			}

		    Log.Warn("The set of messages has already been dispatched.");
			throw new InvalidOperationException("The set of messages has already been dispatched.");
		}

		public DefaultChannelMessageDispatchContext(IMessagingChannel channel, ChannelMessage channelMessage)
		{
			if (channel == null)
			{
			    throw new ArgumentNullException(nameof(channel));
			}

		    if (channelMessage == null)
		    {
		        throw new ArgumentNullException(nameof(channelMessage));
		    }

		    _channel = channel;
			_channelMessage = channelMessage;
		}

		private static readonly ILog Log = LogFactory.Build(typeof(DefaultChannelMessageDispatchContext));
		private readonly ICollection<Uri> _recipients = new LinkedList<Uri>();
		private readonly IMessagingChannel _channel;
		private readonly ChannelMessage _channelMessage;
		private bool _dispatched;
	}
}