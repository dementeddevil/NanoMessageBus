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
		public virtual IMessagingChannel Channel { get; private set; }

		public virtual IDispatchContext WithMessage(object message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var channelMessage = message as ChannelMessage;
			if (channelMessage != null)
				return new DefaultChannelMessageDispatchContext(this._channel, channelMessage);

			Log.Verbose("Adding logical message of type '{0}' for dispatch.", message.GetType());

			this._logicalMessages.Add(message);
			this.MessageCount++;
			return this;
		}
		public virtual IDispatchContext WithMessages(params object[] messages)
		{
			if (messages == null)
				throw new ArgumentNullException(nameof(messages));

			if (messages.Length == 0)
				throw new ArgumentException("The set of messages provided cannot be empty.", nameof(messages));

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
			this._correlationIdentifier = correlationId;
			return this;
		}
		public virtual IDispatchContext WithHeader(string key, string value = null)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));

			var alreadyAdded = this._messageHeaders.ContainsKey(key);

			if (value == null && alreadyAdded)
			{
				Log.Verbose("Removing header '{0}' from dispatch.", key);
				this._messageHeaders.Remove(key);
				this.HeaderCount--;
			}
			else if (value == null)
				return this; // don't add anything
			else
			{
				Log.Verbose("Adding header '{0}' to dispatch.", key);

				if (!alreadyAdded)
					this.HeaderCount++;

				this._messageHeaders[key] = value;
			}

			return this;
		}
		public virtual IDispatchContext WithHeaders(IDictionary<string, string> headers)
		{
			if (headers == null)
				throw new ArgumentNullException(nameof(headers));

			foreach (var item in headers)
				this.WithHeader(item.Key, item.Value);

			return this;
		}
		public virtual IDispatchContext WithRecipient(Uri recipient)
		{
			if (recipient == null)
				throw new ArgumentNullException(nameof(recipient));

			Log.Verbose("Adding recipient '{0}' to dispatch.", recipient);

			this._recipients.Add(recipient);
			return this;
		}
		public virtual IDispatchContext WithState(object state)
		{
			if (state == null)
				throw new ArgumentNullException(nameof(state));

			Log.Verbose("Adding temporary, in-process state of type '{0}' to dispatch.", state.GetType());

			this._applicationState = state;
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
			if (this._channel.CurrentMessage == null)
			{
				Log.Warn("A reply can only be sent because of an incoming message.");
				throw new InvalidOperationException("A reply can only be sent because of an incoming message.");
			}

			if (messages != null)
				this.WithMessages((IEnumerable<object>)messages);

			var message = this._channel.CurrentMessage;
			var incomingReturnAddress = message.ReturnAddress;

			Log.Verbose("Replying to message.");
			if (incomingReturnAddress == null)
				Log.Warn("Incoming message '{0}' contains no return address; unroutable address will be used for reply.", message.MessageId);

			if (this._correlationIdentifier == Guid.Empty)
			{
				Log.Verbose("Using correlation identifier '{0}' from incoming message '{1}'.",
					message.CorrelationId, message.MessageId);
				this._correlationIdentifier = this._channel.CurrentMessage.CorrelationId;
			}

			return this.Dispatch(incomingReturnAddress ?? ChannelEnvelope.UnroutableMessageAddress);
		}
		protected virtual IChannelTransaction Dispatch(params Uri[] targets)
		{
			this.ThrowWhenDispatched();
			this.ThrowWhenNoMessages();

			var message = this._builder.Build(
				this._correlationIdentifier, this._returnAddress, this._messageHeaders, this._logicalMessages);
			var envelope = new ChannelEnvelope(message, targets, this._applicationState ?? this._channel.CurrentMessage);

			Log.Verbose("Dispatching message '{0}' with correlation identifier '{1}' to {2} recipient(s).",
				message.MessageId, message.CorrelationId, targets.Length);

			this.Dispatch(envelope);

			this._dispatched = true;
			this.MessageCount = 0;
			this.HeaderCount = 0;

			return this._channel.CurrentTransaction;
		}
		protected virtual void Dispatch(ChannelEnvelope envelope)
		{
			this._channel.Send(envelope);
		}
		protected virtual IEnumerable<Uri> BuildRecipients()
		{
			if (this._logicalMessages.Count == 0)
				throw new InvalidOperationException("No messages have been specified.");

			var type = this._logicalMessages.First().GetType();
			var discovered = (this._dispatchTable[type] ?? new Uri[0]).Concat(this._recipients).ToArray();
			return discovered.Length == 0 ? new[] { ChannelEnvelope.UnroutableMessageAddress } : discovered;
		}
		protected virtual void ThrowWhenNoMessages()
		{
			if (this._logicalMessages.Count > 0)
				return;

			Log.Warn("No messages have been provided to dispatch.");
			throw new InvalidOperationException("No messages have been provided to dispatch.");
		}
		protected virtual void ThrowWhenDispatched()
		{
			if (!this._dispatched)
				return;
		
			Log.Warn("The set of messages has already been dispatched.");
			throw new InvalidOperationException("The set of messages has already been dispatched.");
		}

		public DefaultDispatchContext(IMessagingChannel channel)
		{
			this.Channel = this._channel = channel;
			this._dispatchTable = this._channel.CurrentConfiguration.DispatchTable;

			var config = channel.CurrentConfiguration;
			this._builder = config.MessageBuilder;
			this._returnAddress = config.ReturnAddress;
		}

		private static readonly ILog Log = LogFactory.Build(typeof(DefaultDispatchContext));
		private readonly IDictionary<string, string> _messageHeaders = new Dictionary<string, string>();
		private readonly ICollection<object> _logicalMessages = new LinkedList<object>();
		private readonly ICollection<Uri> _recipients = new LinkedList<Uri>();
		private readonly IMessagingChannel _channel;
		private readonly IDispatchTable _dispatchTable;
		private readonly IChannelMessageBuilder _builder;
		private readonly Uri _returnAddress;
		private Guid _correlationIdentifier;
		private object _applicationState;
		private bool _dispatched;
	}
}