namespace NanoMessageBus
{
	using System;

	/// <summary>
	/// Represents a set of concurrent workers that perform activities.
	/// </summary>
	/// <typeparam name="TWorkerState">The state held by the worker.</typeparam>
	/// <remarks>
	/// Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
	/// </remarks>
	public interface IWorkerGroup<out TWorkerState> : IDisposable
	{
		/// <summary>
		/// Initiates the stopping and restarting of the activity currently being performed.
		/// </summary>
		/// <exception cref="ObjectDisposedException"></exception>
		void RestartWorkers();

		/// <summary>
		/// Adds a work item to be performed by one of the workers within the worker group.
		/// </summary>
		/// <param name="workItem">
		/// The callback representing the work item to be enqueued and invoked at a later time by one of the workers.
		/// </param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ObjectDisposedException"></exception>
		void EnqueueWork(Action<TWorkerState> workItem);
	}
}