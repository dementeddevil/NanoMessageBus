namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Logging;

	public class DefaultDispatchContext : IDispatchContext
	{
		public virtual int MessageCount { get; private set; }
		public virtual int HeaderCount { get; private set; }

		public virtual IDispatchContext WithMessage(object message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			var channelMessage = message as ChannelMessage;
			if (channelMessage != null)
				return new DefaultChannelMessageDispatchContext(this.channel, channelMessage);

			Log.Verbose("Adding logical message of type '{0}' for dispatch.", message.GetType());

			this.logicalMessages.Add(message);
			this.MessageCount++;
			return this;
		}
		public virtual IDispatchContext WithMessages(params object[] messages)
		{
			if (messages == null)
				throw new ArgumentNullException("messages");

			if (messages.Length == 0)
				throw new ArgumentException("The set of messages provided cannot be empty.", "messages");

			return this.WithMessages((IEnumerable<object>)messages);
		}
		private IDispatchContext WithMessages(IEnumerable<object> messages)
		{
			foreach (var message in messages.Where(x => x != null))
				this.WithMessage(message);

			return this;
		}
		public virtual IDispatchContext WithCorrelationId(Guid correlationId)
		{
			Log.Verbose("Messages will be correlated using identifier '{0}'.", correlationId);
			this.correlationIdentifier = correlationId;
			return this;
		}
		public virtual IDispatchContext WithHeader(string key, string value = null)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			if (value == null)
			{
				Log.Verbose("Removing header '{0}' from dispatch.", key);
				this.messageHeaders.Remove(key);
				this.HeaderCount--;
			}
			else
			{
				Log.Verbose("Adding header '{0}' to dispatch.", key);

				if (!this.messageHeaders.ContainsKey(key))
					this.HeaderCount++;

				this.messageHeaders[key] = value;
			}

			return this;
		}
		public virtual IDispatchContext WithHeaders(IDictionary<string, string> headers)
		{
			if (headers == null)
				throw new ArgumentNullException("headers");

			foreach (var item in headers)
				this.WithHeader(item.Key, item.Value);

			return this;
		}
		public virtual IDispatchContext WithRecipient(Uri recipient)
		{
			if (recipient == null)
				throw new ArgumentNullException("recipient");

			Log.Verbose("Adding recipient '{0}' to dispatch.", recipient);

			this.recipients.Add(recipient);
			return this;
		}
		public virtual IDispatchContext WithState(object state)
		{
			if (state == null)
				throw new ArgumentNullException("state");

			Log.Verbose("Adding temporary, in-process state of type '{0}' to dispatch.", state.GetType());

			this.applicationState = state;
			return this;
		}

		public virtual IChannelTransaction Send(params object[] messages)
		{
			if (messages != null)
				this.WithMessages((IEnumerable<object>)messages);

			Log.Verbose("Sending message to registered recipients.");
			return this.Dispatch(this.BuildRecipients().ToArray());
		}
		public virtual IChannelTransaction Publish(params object[] messages)
		{
			if (messages != null)
				this.WithMessages((IEnumerable<object>)messages);

			Log.Verbose("Publishing message to registered subscribers.");
			return this.Dispatch(this.BuildRecipients().ToArray());
		}
		public virtual IChannelTransaction Reply(params object[] messages)
		{
			if (this.channel.CurrentMessage == null)
			{
				Log.Warn("A reply can only be sent because of an incoming message.");
				throw new InvalidOperationException("A reply can only be sent because of an incoming message.");
			}

			if (messages != null)
				this.WithMessages((IEnumerable<object>)messages);

			var message = this.channel.CurrentMessage;
			var incomingReturnAddress = message.ReturnAddress;

			Log.Verbose("Replying to message.");
			if (incomingReturnAddress == null)
				Log.Warn("Incoming message '{0}' contains no return address; dead-letter address will be used for reply.", message.MessageId);

			if (this.correlationIdentifier == Guid.Empty)
			{
				Log.Verbose("Using correlation identifier '{0}' from incoming message '{1}'.",
					message.CorrelationId, message.MessageId);
				this.correlationIdentifier = this.channel.CurrentMessage.CorrelationId;
			}

			return this.Dispatch(incomingReturnAddress ?? ChannelEnvelope.DeadLetterAddress);
		}
		protected virtual IChannelTransaction Dispatch(params Uri[] targets)
		{
			this.ThrowWhenDispatched();
			this.ThrowWhenNoMessages();

			var message = this.builder.Build(
				this.correlationIdentifier, this.returnAddress, this.messageHeaders, this.logicalMessages);
			var envelope = new ChannelEnvelope(message, targets, this.applicationState ?? this.channel.CurrentMessage);

			Log.Verbose("Dispatching message '{0}' with correlation identifier '{1}' to {2} recipient(s).",
				message.MessageId, message.CorrelationId, targets.Length);

			this.Dispatch(envelope);

			this.dispatched = true;
			this.MessageCount = 0;
			this.HeaderCount = 0;

			return this.channel.CurrentTransaction;
		}
		protected virtual void Dispatch(ChannelEnvelope envelope)
		{
			this.channel.Send(envelope);
		}
		protected virtual IEnumerable<Uri> BuildRecipients()
		{
			if (this.logicalMessages.Count == 0)
				throw new InvalidOperationException("No messages have been specified.");

			var type = this.logicalMessages.First().GetType();
			var discovered = (this.dispatchTable[type] ?? new Uri[0]).Concat(this.recipients).ToArray();
			return discovered.Length == 0 ? new[] { ChannelEnvelope.DeadLetterAddress } : discovered;
		}
		protected virtual void ThrowWhenNoMessages()
		{
			if (this.logicalMessages.Count > 0)
				return;

			Log.Warn("No messages have been provided to dispatch.");
			throw new InvalidOperationException("No messages have been provided to dispatch.");
		}
		protected virtual void ThrowWhenDispatched()
		{
			if (!this.dispatched)
				return;
		
			Log.Warn("The set of messages has already been dispatched.");
			throw new InvalidOperationException("The set of messages has already been dispatched.");
		}

		public DefaultDispatchContext(IMessagingChannel channel)
		{
			this.channel = channel;
			this.dispatchTable = this.channel.CurrentConfiguration.DispatchTable;

			var config = channel.CurrentConfiguration;
			this.builder = config.MessageBuilder;
			this.returnAddress = config.ReturnAddress;
		}

		private static readonly ILog Log = LogFactory.Build(typeof(DefaultDispatchContext));
		private readonly IDictionary<string, string> messageHeaders = new Dictionary<string, string>();
		private readonly ICollection<object> logicalMessages = new LinkedList<object>();
		private readonly ICollection<Uri> recipients = new LinkedList<Uri>();
		private readonly IMessagingChannel channel;
		private readonly IDispatchTable dispatchTable;
		private readonly IChannelMessageBuilder builder;
		private readonly Uri returnAddress;
		private Guid correlationIdentifier;
		private object applicationState;
		private bool dispatched;
	}
}