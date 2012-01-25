namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class DefaultDispatchContext : IDispatchContext
	{
		public virtual IDispatchContext WithMessage(object message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			this.logicalMessages.Add(message);
			return this;
		}
		public virtual IDispatchContext WithMessages(params object[] messages)
		{
			if (messages == null)
				throw new ArgumentNullException("messages");

			if (messages.Length == 0)
				throw new ArgumentException("The set of messages provided cannot be empty.", "messages");

			foreach (var message in messages.Where(x => x != null))
				this.logicalMessages.Add(message);

			return this;
		}
		public virtual IDispatchContext WithCorrelationId(Guid correlationId)
		{
			this.correlationIdentifier = correlationId;
			return this;
		}
		public virtual IDispatchContext WithHeader(string key, string value = null)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			if (value == null)
				this.messageHeaders.Remove(key);
			else
				this.messageHeaders[key] = value;

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

			this.recipients.Add(recipient);
			return this;
		}

		public virtual void Send()
		{
			this.Dispatch(this.BuildRecipients().ToArray());
		}
		public virtual void Publish()
		{
			this.Dispatch(this.BuildRecipients().ToArray());
		}
		public virtual void Reply()
		{
			if (this.correlationIdentifier == Guid.Empty)
				this.correlationIdentifier = this.delivery.CurrentMessage.CorrelationId;

			this.Dispatch(this.delivery.CurrentMessage.ReturnAddress ?? ChannelEnvelope.DeadLetterAddress);
		}
		protected virtual void Dispatch(params Uri[] targets)
		{
			this.ThrowWhenDispatched();
			this.ThrowWhenNoMessages();

			var message = this.BuildMessage();
			var envelope = new ChannelEnvelope(message, targets);
			this.delivery.Send(envelope);
			this.dispatched = true;
		}
		protected virtual IEnumerable<Uri> BuildRecipients()
		{
			var type = this.logicalMessages.First().GetType();
			var discovered = (this.dispatchTable[type] ?? new Uri[0]).Concat(this.recipients).ToArray();

			return discovered.Length == 0 ? new[] { ChannelEnvelope.DeadLetterAddress } : discovered;
		}
		protected virtual ChannelMessage BuildMessage()
		{
			return new ChannelMessage(
				Guid.NewGuid(),
				this.correlationIdentifier,
				this.delivery.CurrentConfiguration.ReturnAddress,
				this.messageHeaders,
				this.logicalMessages)
			{
				Persistent = true,
				Expiration = SystemTime.UtcNow.AddDays(3)
			};
		}
		protected virtual void ThrowWhenNoMessages()
		{
			if (this.logicalMessages.Count == 0)
				throw new InvalidOperationException("No messages have been provided to dispatch.");
		}
		protected virtual void ThrowWhenDispatched()
		{
			if (this.dispatched)
				throw new InvalidOperationException("The set of messages has already been dispatched.");
		}

		public DefaultDispatchContext(IDeliveryContext delivery, IDispatchTable dispatchTable)
		{
			this.delivery = delivery;
			this.dispatchTable = dispatchTable;
		}

		private readonly IDictionary<string, string> messageHeaders = new Dictionary<string, string>();
		private readonly ICollection<object> logicalMessages = new LinkedList<object>();
		private readonly ICollection<Uri> recipients = new LinkedList<Uri>();
		private readonly IDeliveryContext delivery;
		private readonly IDispatchTable dispatchTable;
		private Guid correlationIdentifier;
		private bool dispatched;
	}
}