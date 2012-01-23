namespace NanoMessageBus
{
	using System;

	public class DefaultChannelGroupFactory
	{
		public virtual IChannelGroup Build(IChannelConnector connector, IChannelGroupConfiguration configuration)
		{
			if (connector == null)
				throw new ArgumentNullException("connector");

			if (configuration == null)
				throw new ArgumentNullException("configuration");

			var workers = new TaskWorkerGroup<IMessagingChannel>(configuration.MinWorkers, configuration.MaxWorkers);
			return new DefaultChannelGroup(connector, configuration, workers);
		}
	}
}