namespace NanoMessageBus
{
	using System;

	/// <summary>
	/// Represents the minimum configuration necessary to establish a channel group.
	/// </summary>
	/// <remarks>
	/// Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
	/// </remarks>
	public interface IChannelGroupConfiguration
	{
		/// <summary>
		/// Gets the value which uniquely identifies the named configuration or channel group.
		/// </summary>
		string GroupName { get; }

		/// <summary>
		/// Gets a value indicating whether the particular configuration should support asynchronous operations.
		/// </summary>
		bool Synchronous { get; }

		/// <summary>
		/// Gets a value indicating whether the connection is configured for dispatch or full duplex.
		/// </summary>
		bool DispatchOnly { get; }
		
		/// <summary>
		/// Gets a value indicating the minimum number of workers to be allocated for work.
		/// </summary>
		int MinWorkers { get; }

		/// <summary>
		/// Gets a value indicating the maximum allowable number of workers to be allocated for work.
		/// </summary>
		int MaxWorkers { get; }

		/// <summary>
		/// Gets the URI representing the address to which all reply messages will be sent.
		/// </summary>
		Uri ReturnAddress { get; }

		/// <summary>
		/// Gets a reference to the object instance used to build new, outbound channel messages.
		/// </summary>
		IChannelMessageBuilder MessageBuilder { get; }

		/// <summary>
		/// Gets the length of time to await the receipt of a message from a channel before resume other work.
		/// </summary>
		TimeSpan ReceiveTimeout { get; }

		/// <summary>
		/// Gets an optional reference to resolver used to manage dependencies.
		/// </summary>
		IDependencyResolver DependencyResolver { get; }

		/// <summary>
		/// Gets a reference to the dispatch table to determine the appropriate recipients for a given type of message.
		/// </summary>
		IDispatchTable DispatchTable { get; }
	}
}