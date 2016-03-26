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
			get { return this._connector.CurrentState; }
		}
		public virtual IEnumerable<IChannelGroupConfiguration> ChannelGroups
		{
			get { return this._connector.ChannelGroups; }
		}
		public virtual IMessagingChannel Connect(string channelGroup)
		{
			Log.Debug("Attempting to open a channel for group '{0}'.", channelGroup);
			var channel = this._connector.Connect(channelGroup);
			var auditors = this.ResolveAuditors(channel);

			if (auditors.Count > 0)
			{
				Log.Verbose("{0} auditors configured; creating new AuditChannel.", auditors.Count);
				return new AuditChannel(channel, auditors);
			}

			Log.Info("No auditors have been configured, no further attempts to audit will occur.");
			this._emptyFactory = true;
			return channel;
		}
		protected virtual ICollection<IMessageAuditor> ResolveAuditors(IMessagingChannel channel)
		{
			if (this._emptyFactory)
				return new IMessageAuditor[0];

			return this._auditorFactory(channel).Where(x => x != null).ToArray();
		}

		public AuditConnector(IChannelConnector connector, Func<IMessagingChannel, IEnumerable<IMessageAuditor>> auditorFactory)
		{
			if (connector == null)
				throw new ArgumentNullException(nameof(connector));

			if (auditorFactory == null)
				throw new ArgumentNullException(nameof(auditorFactory));

			this._connector = connector;
			this._auditorFactory = auditorFactory;
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
			this._connector.TryDispose();
		}

		private static readonly ILog Log = LogFactory.Build(typeof(AuditConnector));
		private readonly IChannelConnector _connector;
		private readonly Func<IMessagingChannel, IEnumerable<IMessageAuditor>> _auditorFactory;
		private bool _emptyFactory;
	}
}