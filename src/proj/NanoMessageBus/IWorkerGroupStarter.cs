namespace NanoMessageBus
{
	using System;

	/// <summary>
	/// Provides the ability to start sets of workers that concurrently perform the specified activity.
	/// </summary>
	/// <typeparam name="TWorkerState">The state held by the worker.</typeparam>
	/// <remarks>
	/// Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
	/// *IMPORTANT* Each channel group should receive a unique instance of this class.
	/// </remarks>
	public interface IWorkerGroupStarter<TWorkerState>
	{
		/// <summary>
		/// Initializes the factory and causes all future worker groups to use the callbacks provided.
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		/// <param name="state">The callback used to get the state of the worker.</param>
		/// <param name="restart">The callback used to restart the workers.</param>
		void Initialize(Func<TWorkerState> state, Func<TWorkerState, bool> restart);

		/// <summary>
		/// Builds a worker group which starts performing the activity specified.
		/// </summary>
		/// <param name="activity">The activity to be performed by the workers.</param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="InvalidOperationException"></exception>
		/// <returns>A new worker group which has been started and is performing the specified activity.</returns>
		IWorkerGroup<TWorkerState> StartActivity(Action<TWorkerState> activity);

		/// <summary>
		/// Builds a worker group which watches a work item queue.
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		/// <returns>A new worker group which has been started and is watching a work item queue.</returns>
		IWorkerGroup<TWorkerState> StartQueue();
	}
}