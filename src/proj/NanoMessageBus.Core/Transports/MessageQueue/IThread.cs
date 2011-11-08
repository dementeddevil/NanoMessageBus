namespace NanoMessageBus.Transports.MessageQueue
{
	public interface IThread
	{
		bool IsAlive { get; }
		string Name { get; }
		void Start();
		void Abort();
	}
}