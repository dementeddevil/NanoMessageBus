namespace NanoMessageBus
{
	using System;

	/// <summary>
	/// Represents an exception which indicates that any additional logic for the current message handler should be aborted and skipped.
	/// </summary>
	[Serializable]
	public class AbortCurrentHandlerException : Exception
	{
		public AbortCurrentHandlerException(string message) : base(message)
		{
		}
	}
}