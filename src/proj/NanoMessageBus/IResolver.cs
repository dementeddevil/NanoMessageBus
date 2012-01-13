namespace NanoMessageBus
{
	using System;

	public interface IResolver : IDisposable
	{
		IResolver CreateNestedResolver();
	}
}