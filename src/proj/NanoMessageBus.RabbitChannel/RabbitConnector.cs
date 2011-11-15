namespace NanoMessageBus.RabbitChannel
{
	using System;
	using System.Collections.Generic;

	public class RabbitConnector : IChannelConnector
	{
		public virtual ConnectionState CurrentState
		{
			get { return 0; }
		}
		public virtual IEnumerable<IChannelConfiguration> ChannelGroups
		{
			get { return new IChannelConfiguration[0]; }
		}
		public virtual IMessagingChannel Connect(string channelGroup)
		{
			return null;
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
		}
	}
}