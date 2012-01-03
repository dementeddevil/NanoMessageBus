namespace NanoMessageBus
{
	using System;

	/// <summary>
	/// Represents a set of concurrent workers that perform activities against the configured number of messaging channels.
	/// </summary>
	/// <remarks>
	/// Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
	/// </remarks>
	public interface IWorkerGroup : IDisposable
	{
		// Notes: CG's Initialize can start the worker group with the activity of opening the connection for the first time
		// once that works, it stops, and then re-starts with the activity of receiving a message (where an exception would
		// stop the worker group and then re-start with another activity of retrying the connection every so often)
		// one thing to be careful of is being in the middle of receiving and then stopping/starting to quickly where the
		// already existing workers also thrown an exception and do the exact same thing
		// we need to figure out a way to synchronize the behavior there--perhaps with a status indicator update surrounded
		// by a simple lock *OR* perhaps the worker group keeps track of the worker that threw the exception
		// and if it was one of the stopped workers, it consumes the exception

		/// <summary>
		/// Instructs the worker group to start performing the specified activity against the underlying set of channels.
		/// </summary>
		/// <param name="activity">The activity to be performed by the configured number of workers.</param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="ObjectDisposedException"></exception>
		void Start(Action activity);

		/// <summary>
		/// Instructs the worker group to start a single worker performing the activity provided.
		/// </summary>
		/// <param name="activity">The activity to be performed by a single worker.</param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="ObjectDisposedException"></exception>
		void StartSingleWorker(Action activity);

		/// <summary>
		/// Instructs the worker group to start watching the work item queue.
		/// </summary>
		void StartWorkItemQueue();

		/// <summary>
		/// Initiates the stopping of all activities currently being performed without removing any uncompleted work items.
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="ObjectDisposedException"></exception>
		void Stop();

		/// <summary>
		/// Adds a work item to be performed as part of the default activity (the activity when none is specified at start).
		/// </summary>
		/// <param name="workItem">The work item to be enqueued for execution.</param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ObjectDisposedException"></exception>
		void Add(Action<IMessagingChannel> workItem);
	}
}