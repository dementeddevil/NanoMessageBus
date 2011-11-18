namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;

	/// <summary>
	/// Represents a message with a collection of recipients to be dispatched.
	/// </summary>
	public class ChannelEnvelope
	{
		/// <summary>
		/// Represents a loopback address used for dispatch a message to a local endpoint.
		/// </summary>
		public static readonly Uri LoopbackAddress = new Uri("loopback://localhost/");

		/// <summary>
		/// Gets the message to be dispatched.
		/// </summary>
		public ChannelMessage Message { get; private set; }

		/// <summary>
		/// Gets the collection of recipients to which the message will be sent.
		/// </summary>
		public ICollection<Uri> Recipients { get; private set; }

		/// <summary>
		/// Initializes a new instance of the ChannelEnvelope class.
		/// </summary>
		/// <param name="message">The message to be dispatched</param>
		/// <param name="recipients">The collection of recipients to which the message will be sent</param>
		public ChannelEnvelope(ChannelMessage message, IEnumerable<Uri> recipients)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			if (recipients == null)
				throw new ArgumentNullException("recipients");

			this.Message = message;
			this.Recipients = new ReadOnlyCollection<Uri>(recipients.Where(x => x != null).ToArray());

			if (this.Recipients.Count == 0)
				throw new ArgumentException("No recipients were provided.", "recipients");
		}
	}
}