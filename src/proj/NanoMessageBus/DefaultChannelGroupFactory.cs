namespace NanoMessageBus
{
	public class DefaultChannelGroupFactory
	{
		public virtual IChannelGroup Build(IChannelConnector connector, IChannelConfiguration configuration)
		{
			// TODO
			return new DefaultChannelGroup(); 
		}
	}
}