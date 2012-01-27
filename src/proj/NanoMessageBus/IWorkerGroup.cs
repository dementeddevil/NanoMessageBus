namespace NanoMessageBus
{
	using System;

	/// <summary>
	/// Represents a set of concurrent workers that perform activities.
	/// </summary>
	/// <typeparam name="T">The state held by the worker.</typeparam>
	/// <remarks>
	/// Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
	/// </remarks>
	public interface IWorkerGroup<T> : IDisposable
		where T : class, IDisposable
	{
		/// <summary>
		/// Initializes the factory and causes all future worker groups to use the callbacks provided.
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		/// <param name="state">The callback used to get the state of the worker.</param>
		/// <param name="restart">The callback used to restart the workers.</param>
		void Initialize(Func<T> state, Func<bool> restart);

		/// <summary>
		/// Builds a worker group which starts performing the activity specified.
		/// </summary>
		/// <param name="activity">The activity to be performed by the workers.</param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="InvalidOperationException"></exception>
		void StartActivity(Action<IWorkItem<T>> activity);

		/// <summary>
		/// Builds a worker group which watches a work item queue.
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		void StartQueue();

		/// <summary>
		/// Initiates the stopping and restarting of the activity currently being performed.
		/// </summary>
		/// <exception cref="ObjectDisposedException"></exception>
		void Restart();

		/// <summary>
		/// Adds a work item to be performed by one of the workers within the worker group.  Work items
		/// can safely be added at any time during the lifetime of the object instance.
		/// </summary>
		/// <param name="workItem">
		/// The callback representing the work item to be enqueued and invoked at a later time by one of the workers.
		/// </param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ObjectDisposedException"></exception>
		/// <returns>If the value was enqueued, returns true; otherwise false.</returns>
		bool Enqueue(Action<IWorkItem<T>> workItem);
	}
}