namespace NanoMessageBus.Channels
{
	using System;

	public interface IMessagingChannel : IDisposable
	{
		MessageEnvelope CurrentMessage { get; }
		IChannelTransaction CurrentTransaction { get; }

		void Send(MessageEnvelope envelope, params Uri[] destinations);
		MessageEnvelope Receive(TimeSpan timeout);
	}
}