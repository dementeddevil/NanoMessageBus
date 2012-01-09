namespace NanoMessageBus
{
	public class DefaultChannelGroupFactory
	{
		public virtual IChannelGroup Build(IChannelConnector connector, IChannelGroupConfiguration configuration)
		{
			var workers = new TaskWorkerGroup<IMessagingChannel>(configuration.MinWorkers, configuration.MaxWorkers);
			return new DefaultChannelGroup(connector, configuration, workers);
		}
	}
}