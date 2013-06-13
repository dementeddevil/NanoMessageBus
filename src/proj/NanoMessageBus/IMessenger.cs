namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;

	public interface IMessenger : IDisposable
	{
		void Dispatch(object message, IDictionary<string, string> headers = null, object state = null);
		void Commit();
	}
}