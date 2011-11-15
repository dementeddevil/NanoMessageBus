namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;

	public interface IChannelConnector : IDisposable
	{
		ICollection<string> Groups { get; }

		IEnumerable<IChannelGroup> Connect();

		event EventHandler ConnectionUnavailable;
	}
}