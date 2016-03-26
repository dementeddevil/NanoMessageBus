using System.Threading.Tasks;

namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using Logging;

	public class DefaultChannelMessageHandler : IMessageHandler<ChannelMessage>
	{
		public virtual async Task HandleAsync(ChannelMessage message)
		{
			ICollection<object> unhandled = new List<object>(message.Messages.Count);

			try
			{
				Log.Verbose("Handling channel message '{0}' which contains '{1}' logical messages.",
					message.MessageId, message.Messages.Count);

				var handled = 0;
			    while (message.MoveNext() && this._context.ContinueHandling)
			    {
			        handled += await this.Route(message.ActiveMessage, unhandled).ConfigureAwait(false);
			    }

			    if (handled == 0)
			    {
			        unhandled.Clear();
			    }

			    if (this._context.ContinueHandling && (handled == 0 || unhandled.Count > 0))
			    {
			        await this.ForwardToUnhandledAddress(message, unhandled).ConfigureAwait(false);
			    }
			}
			finally
			{
				message.Reset();
			}
		}

		protected virtual Task ForwardToUnhandledAddress(ChannelMessage message, ICollection<object> messages)
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

			this._context.PrepareDispatch()
				.WithMessage(message)
				.WithRecipient(ChannelEnvelope.UnhandledMessageAddress)
				.Send();

		    return Task.FromResult(true);
		}

		private async Task<int> Route(object message, ICollection<object> unhandled)
		{
			var count = await this._routes.Route(this._context, message).ConfigureAwait(false);
		    if (count == 0)
		    {
		        unhandled.Add(message);
		    }
			return count;
		}

		public DefaultChannelMessageHandler(IHandlerContext context, IRoutingTable routes)
		{
			if (context == null)
				throw new ArgumentNullException(nameof(context));

			if (routes == null)
				throw new ArgumentNullException(nameof(routes));

			this._context = context;
			this._routes = routes;
		}

		private static readonly ILog Log = LogFactory.Build(typeof(DefaultChannelMessageHandler));
		private readonly IHandlerContext _context;
		private readonly IRoutingTable _routes;
	}
}