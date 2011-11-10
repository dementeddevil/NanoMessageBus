namespace NanoMessageBus.Handlers
{
	using System;

	public sealed class NullUnitOfWork : IHandleUnitOfWork
	{
		public void Register(Action callback)
		{
			if (callback != null)
				callback();
		}
		public void Complete()
		{
		}
		public void Clear()
		{
		}

		public void Dispose()
		{
		}
	}
}