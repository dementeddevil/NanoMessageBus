namespace NanoMessageBus
{
	public class DefaultChannelGroupFactory
	{
		public virtual IChannelGroup Build(IChannelConnector connector, IChannelConfiguration configuration)
		{
			return new DefaultChannelGroup(connector, configuration); 
		}
	}
}