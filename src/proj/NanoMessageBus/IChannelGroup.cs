namespace NanoMessageBus
{
	public interface IChannelGroup
	{
		string Name { get; }
		bool DispatchOnly { get; }
		int MinWorkers { get; }
		int MaxWorkers { get; }

		/// <summary>
		/// Adds the message envelope provided to the shared outbound dispatch queue to be asynchronously be
		/// dispatched any of the channels within the group.
		/// </summary>
		/// <param name="envelope">The message envelope to be dispatched.</param>
		void Dispatch(EnvelopeMessage envelope);
	}
}