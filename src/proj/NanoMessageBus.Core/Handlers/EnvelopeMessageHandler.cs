namespace NanoMessageBus.Handlers
{
	using System.Linq;
	using Logging;

	public class EnvelopeMessageHandler : IHandleMessages<EnvelopeMessage>
	{
		public virtual void Handle(EnvelopeMessage message)
		{
			Log.Debug(Diagnostics.LogicalMessageCount, message.LogicalMessages.Count);
			foreach (var logicalMessage in message.LogicalMessages.Where(x => x != null))
				this.HandleLogicalMessage(logicalMessage);
		}
		private void HandleLogicalMessage(object message)
		{
			Log.Debug(Diagnostics.RoutingLogicalMessageToHandlers, message.GetType());

			var handlers = this.handlerTable.GetHandlers(message);
			foreach (var handler in handlers.TakeWhile(handler => this.context.ContinueProcessing))
				handler.Handle(message);

			Log.Debug(Diagnostics.LogicalMessageHandled, message.GetType());
		}

		public EnvelopeMessageHandler(ITrackMessageHandlers handlerTable, IMessageContext context)
		{
			this.handlerTable = handlerTable;
			this.context = context;
		}

		private static readonly ILog Log = LogFactory.BuildLogger(typeof(EnvelopeMessageHandler));
		private readonly ITrackMessageHandlers handlerTable;
		private readonly IMessageContext context;
	}
}