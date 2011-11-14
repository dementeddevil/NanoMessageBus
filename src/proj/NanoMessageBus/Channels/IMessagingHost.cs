namespace NanoMessageBus.Channels
{
	using System;

	public interface IMessagingHost : IDisposable
	{
		void Listen();
		void Pause();
	}
}