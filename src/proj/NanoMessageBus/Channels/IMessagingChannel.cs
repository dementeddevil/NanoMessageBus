namespace NanoMessageBus.Channels
{
	using System;

	public interface IMessagingChannel : IDisposable
	{
		EnvelopeMessage CurrentMessage { get; }
		IChannelTransaction CurrentTransaction { get; }

		void Send(EnvelopeMessage envelope, params Uri[] destinations);
		EnvelopeMessage Receive(TimeSpan timeout);
	}
}