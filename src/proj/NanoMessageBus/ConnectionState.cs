namespace NanoMessageBus
{
	/// <summary>
	/// Represents the state of the underlying connection at various critical points.
	/// </summary>
	public enum ConnectionState
	{
		/// <summary>
		/// The connection is closed and no operations can be performed.
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
		/// The connection is available and attempts to re-open the connection are being made.
		/// </summary>
		Unavailable,

		/// <summary>
		/// Indicates that the current security credentials are incorrect or insufficient for
		/// the request privileges on the account.
		/// </summary>
		Unauthorized
	}
}