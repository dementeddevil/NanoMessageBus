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
			{
			    throw new ArgumentNullException(nameof(message));
			}

		    var channelMessage = message as ChannelMessage;
			if (channelMessage != null)
			{
			    return new DefaultChannelMessageDispatchContext(_channel, channelMessage);
			}

		    Log.Verbose("Adding logical message of type '{0}' for dispatch.", message.GetType());

			_logicalMessages.Add(message);
			MessageCount++;
			return this;
		}
		public virtual IDispatchContext WithMessages(params object[] messages)
		{
			if (messages == null)
			{
			    throw new ArgumentNullException(nameof(messages));
			}

		    if (messages.Length == 0)
		    {
		        throw new ArgumentException("The set of messages provided cannot be empty.", nameof(messages));
		    }

		    return WithMessages((IEnumerable<object>)messages);
		}
		private IDispatchContext WithMessages(IEnumerable<object> messages)
		{
			foreach (var message in messages.Where(x => x != null))
			{
			    WithMessage(message);
			}

		    return this;
		}
		public virtual IDispatchContext WithCorrelationId(Guid correlationId)
		{
			Log.Verbose("Messages will be correlated using identifier '{0}'.", correlationId);
			_correlationIdentifier = correlationId;
			return this;
		}
		public virtual IDispatchContext WithHeader(string key, string value = null)
		{
			if (key == null)
			{
			    throw new ArgumentNullException(nameof(key));
			}

		    var alreadyAdded = _messageHeaders.ContainsKey(key);

			if (value == null && alreadyAdded)
			{
				Log.Verbose("Removing header '{0}' from dispatch.", key);
				_messageHeaders.Remove(key);
				HeaderCount--;
			}
			else if (value == null)
			{
			    return this; // don't add anything
			}
			else
			{
				Log.Verbose("Adding header '{0}' to dispatch.", key);

				if (!alreadyAdded)
				{
				    HeaderCount++;
				}

			    _messageHeaders[key] = value;
			}

			return this;
		}
		public virtual IDispatchContext WithHeaders(IDictionary<string, string> headers)
		{
			if (headers == null)
			{
			    throw new ArgumentNullException(nameof(headers));
			}

		    foreach (var item in headers)
		    {
		        WithHeader(item.Key, item.Value);
		    }

		    return this;
		}
		public virtual IDispatchContext WithRecipient(Uri recipient)
		{
			if (recipient == null)
			{
			    throw new ArgumentNullException(nameof(recipient));
			}

		    Log.Verbose("Adding recipient '{0}' to dispatch.", recipient);

			_recipients.Add(recipient);
			return this;
		}
		public virtual IDispatchContext WithState(object state)
		{
			if (state == null)
			{
			    throw new ArgumentNullException(nameof(state));
			}

		    Log.Verbose("Adding temporary, in-process state of type '{0}' to dispatch.", state.GetType());

			_applicationState = state;
			return this;
		}

		public virtual IChannelTransaction Send(params object[] messages)
		{
			if (messages != null)
			{
			    WithMessages((IEnumerable<object>)messages);
			}

		    Log.Verbose("Sending message to registered recipients.");
			return Dispatch(BuildRecipients().ToArray());
		}
		public virtual IChannelTransaction Publish(params object[] messages)
		{
			if (messages != null)
			{
			    WithMessages((IEnumerable<object>)messages);
			}

		    Log.Verbose("Publishing message to registered subscribers.");
			return Dispatch(BuildRecipients().ToArray());
		}
		public virtual IChannelTransaction Reply(params object[] messages)
		{
			if (_channel.CurrentMessage == null)
			{
				Log.Warn("A reply can only be sent because of an incoming message.");
				throw new InvalidOperationException("A reply can only be sent because of an incoming message.");
			}

			if (messages != null)
			{
			    WithMessages((IEnumerable<object>)messages);
			}

		    var message = _channel.CurrentMessage;
			var incomingReturnAddress = message.ReturnAddress;

			Log.Verbose("Replying to message.");
			if (incomingReturnAddress == null)
			{
			    Log.Warn("Incoming message '{0}' contains no return address; unroutable address will be used for reply.", message.MessageId);
			}

		    if (_correlationIdentifier == Guid.Empty)
			{
				Log.Verbose("Using correlation identifier '{0}' from incoming message '{1}'.",
					message.CorrelationId, message.MessageId);
				_correlationIdentifier = _channel.CurrentMessage.CorrelationId;
			}

			return Dispatch(incomingReturnAddress ?? ChannelEnvelope.UnroutableMessageAddress);
		}
		protected virtual IChannelTransaction Dispatch(params Uri[] targets)
		{
			ThrowWhenDispatched();
			ThrowWhenNoMessages();

			var message = _builder.Build(
				_correlationIdentifier, _returnAddress, _messageHeaders, _logicalMessages);
			var envelope = new ChannelEnvelope(message, targets, _applicationState ?? _channel.CurrentMessage);

			Log.Verbose("Dispatching message '{0}' with correlation identifier '{1}' to {2} recipient(s).",
				message.MessageId, message.CorrelationId, targets.Length);

			Dispatch(envelope);

			_dispatched = true;
			MessageCount = 0;
			HeaderCount = 0;

			return _channel.CurrentTransaction;
		}
		protected virtual void Dispatch(ChannelEnvelope envelope)
		{
			_channel.SendAsync(envelope);
		}
		protected virtual IEnumerable<Uri> BuildRecipients()
		{
			if (_logicalMessages.Count == 0)
			{
			    throw new InvalidOperationException("No messages have been specified.");
			}

		    var type = _logicalMessages.First().GetType();
			var discovered = (_dispatchTable[type] ?? new Uri[0]).Concat(_recipients).ToArray();
			return discovered.Length == 0 ? new[] { ChannelEnvelope.UnroutableMessageAddress } : discovered;
		}
		protected virtual void ThrowWhenNoMessages()
		{
			if (_logicalMessages.Count > 0)
			{
			    return;
			}

		    Log.Warn("No messages have been provided to dispatch.");
			throw new InvalidOperationException("No messages have been provided to dispatch.");
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

		public DefaultDispatchContext(IMessagingChannel channel)
		{
			Channel = _channel = channel;
			_dispatchTable = _channel.CurrentConfiguration.DispatchTable;

			var config = channel.CurrentConfiguration;
			_builder = config.MessageBuilder;
			_returnAddress = config.ReturnAddress;
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