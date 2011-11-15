namespace NanoMessageBus
{
	/// <summary>
	/// Represents the minimum configuration necessary to establish a channel group.
	/// </summary>
	public interface IChannelConfiguration
	{
		/// <summary>
		/// Gets the value which uniquely identifies the named configuration or channel group.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets a value indicating whether the connection is configured for dispatch or full duplex.
		/// </summary>
		bool DispatchOnly { get; }
		
		/// <summary>
		/// Gets a value indicating the minimum number of worker threads to be allocated for work.
		/// </summary>
		int MinThreads { get; }

		/// <summary>
		/// Gets a value indicating the maximum allowable number of worker threads to be allocated for work.
		/// </summary>
		int MaxThreads { get; }
	}
}