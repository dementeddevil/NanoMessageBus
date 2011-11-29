namespace NanoMessageBus
{
	/// <summary>
	/// Represents the state of the underlying connection at various critical points.
	/// </summary>
	public enum ConnectionState
	{
		/// <summary>
		/// The connection is closed and no operations can be performed until the connection is reestablished.
		/// </summary>
		Closed,

		/// <summary>
		/// The connection is opening and being initialized.
		/// </summary>
		Opening,

		/// <summary>
		/// The connection is open and ready for work.
		/// </summary>
		Open,

		/// <summary>
		/// The connection is shutting down and performing any cleanup necessary.
		/// </summary>
		Closing,

		/// <summary>
		/// The endpoint was previously available and attempts to re-open the connection are being made.
		/// </summary>
		Disconnected,

		/// <summary>
		/// Indicates that the current security credentials are incorrect.
		/// </summary>
		Unauthenticated,

		/// <summary>
		/// Indicates that the current security context does not contain the necessary privileges.
		/// </summary>
		Unauthorized
	}
}