namespace NanoMessageBus.Channels
{
	using System;

	public interface IChannelTransaction : IDisposable
	{
		void Register(Action callback);

		void Complete();
		void Rollback();
	}
}