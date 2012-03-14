namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Logging;

	public class DefaultChannelMessageHandler : IMessageHandler<ChannelMessage>
	{
		public virtual void Handle(ChannelMessage message)
		{
			ICollection<object> unhandled = new List<object>(message.Messages.Count);

			Log.Verbose("Handling channel message '{0}' which contains '{1}' logical messages.",
				message.MessageId, message.Messages.Count);

			var handled = message.Messages
				.TakeWhile(x => this.context.ContinueHandling)
				.Sum(x => this.Route(x, unhandled));

			if (handled == 0)
				unhandled.Clear();

			if (this.context.ContinueHandling && (handled == 0 || unhandled.Count > 0))
				this.ForwardToDeadLetterAddress(message, unhandled);
		}
		private int Route(object message, ICollection<object> unhandled)
		{
			var count = this.routes.Route(this.context, message);
			if (count == 0)
				unhandled.Add(message);
			return count;
		}
		protected virtual void ForwardToDeadLetterAddress(ChannelMessage message, ICollection<object> messages)
		{
			Log.Debug("Channel message '{0}' contained unhandled messages.", message.MessageId);

			if (messages.Count == 0)
				Log.Debug("Forwarding entire channel message '{0}' to dead-letter address.", message.MessageId);
			else
				message = new ChannelMessage(
					Guid.NewGuid(),
					message.CorrelationId,
					message.ReturnAddress,
					message.Headers,
					messages);

			this.context.PrepareDispatch()
				.WithMessage(message)
				.WithRecipient(ChannelEnvelope.DeadLetterAddress)
				.Send();
		}

		public DefaultChannelMessageHandler(IHandlerContext context, IRoutingTable routes)
		{
			if (context == null)
				throw new ArgumentNullException("context");

			if (routes == null)
				throw new ArgumentNullException("routes");

			this.context = context;
			this.routes = routes;
		}

		private static readonly ILog Log = LogFactory.Build(typeof(DefaultChannelMessageHandler));
		private readonly IHandlerContext context;
		private readonly IRoutingTable routes;
	}
}