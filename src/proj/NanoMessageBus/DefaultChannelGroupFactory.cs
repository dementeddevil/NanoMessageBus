namespace NanoMessageBus
{
	public class DefaultChannelGroupFactory
	{
		public virtual IChannelGroup Build(IChannelConnector connector, IChannelGroupConfiguration configuration)
		{
			return new DefaultChannelGroup(connector, configuration); 
		}
	}
}