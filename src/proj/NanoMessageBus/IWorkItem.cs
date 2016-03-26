using System.Threading.Tasks;

namespace NanoMessageBus
{
	using System;

	/// <summary>
	/// Provides the ability to track state for activity being performed.
	/// </summary>
	/// <typeparam name="T">The state held by the worker.</typeparam>
	public interface IWorkItem<out T>
		where T : class, IDisposable
	{
		/// <summary>
		/// Gets the value which indicates the number of active workers performing the activity.
		/// </summary>
		int ActiveWorkers { get; }

		/// <summary>
		/// Gets the state associated with the activity.
		/// </summary>
		T State { get; }

		/// <summary>
		/// Instructs the worker to perform the operation indicated.
		/// </summary>
		/// <param name="operation">The operation to be performed by the worker.</param>
		Task PerformOperation(Func<Task> operation);
	}
}