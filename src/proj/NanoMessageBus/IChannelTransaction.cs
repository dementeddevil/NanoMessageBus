namespace NanoMessageBus
{
	using System;

	/// <summary>
	/// For supported channels, represents a set of messaging activies on the channel (such as receive and send)
	/// that happen as a unit or not all.
	/// </summary>
	/// <remarks>
	/// Instances of this class are single threaded and should not be shared between threads.
	/// </remarks>
	public interface IChannelTransaction : IDisposable
	{
		/// <summary>
		/// Gets a value indicating whether the transaction has been committed, rolled back, or disposed;
		/// </summary>
		bool Finished { get; }

		/// <summary>
		/// Registers the associated action with the transaction.
		/// </summary>
		/// <param name="callback">The action to be invoked when the transaction is committed.</param>
		void Register(Action callback);

		/// <summary>
		/// Invokes the registered callbacks to mark the transaction as complete.
		/// </summary>
		void Commit();

		/// <summary>
		/// Rolls back any work performed.
		/// </summary>
		void Rollback();
	}
}