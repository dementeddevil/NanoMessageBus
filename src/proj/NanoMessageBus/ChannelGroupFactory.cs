namespace NanoMessageBus
{
	/// <summary>
	/// Provides the ability to construct a new channel group.
	/// </summary>
	/// <param name="connector">The connector responsible for the channels inside the group.</param>
	/// <param name="configuration">The configuration of the channel group to be constructed.</param>
	/// <returns>A new object instance of the named channel group specified.</returns>
	public delegate IChannelGroup ChannelGroupFactory(
		IChannelConnector connector, IChannelConfiguration configuration);
}