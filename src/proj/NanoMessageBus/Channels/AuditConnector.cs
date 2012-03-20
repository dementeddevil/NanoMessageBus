namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Logging;

	public class AuditConnector : IChannelConnector
	{
		public virtual ConnectionState CurrentState
		{
			get { return this.connector.CurrentState; }
		}
		public virtual IEnumerable<IChannelGroupConfiguration> ChannelGroups
		{
			get { return this.connector.ChannelGroups; }
		}
		public virtual IMessagingChannel Connect(string channelGroup)
		{
			Log.Debug("Attempting to open a channel for group '{0}'.", channelGroup);
			var channel = this.connector.Connect(channelGroup);
			var auditors = this.ResolveAuditors(channel);

			if (auditors.Count > 0)
			{
				Log.Verbose("{0} auditors configured; creating new AuditChannel.", auditors.Count);
				return new AuditChannel(channel, auditors);
			}

			Log.Info("No auditors have been configured, no further attempts to audit will occur.");
			this.emptyFactory = true;
			return channel;
		}
		protected virtual ICollection<IMessageAuditor> ResolveAuditors(IMessagingChannel channel)
		{
			if (this.emptyFactory)
				return new IMessageAuditor[0];

			return this.auditorFactory(channel).Where(x => x != null).ToArray();
		}

		public AuditConnector(IChannelConnector connector, Func<IMessagingChannel, IEnumerable<IMessageAuditor>> auditorFactory)
		{
			if (connector == null)
				throw new ArgumentNullException("connector");

			if (auditorFactory == null)
				throw new ArgumentNullException("auditorFactory");

			this.connector = connector;
			this.auditorFactory = auditorFactory;
		}
		~AuditConnector()
		{
			this.Dispose(false);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
				return;

			Log.Debug("Disposing the underlying connection.");
			this.connector.Dispose();
		}

		private static readonly ILog Log = LogFactory.Build(typeof(AuditConnector));
		private readonly IChannelConnector connector;
		private readonly Func<IMessagingChannel, IEnumerable<IMessageAuditor>> auditorFactory;
		private bool emptyFactory;
	}
}