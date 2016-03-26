﻿namespace NanoMessageBus
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
		/// Represents a loopback address used for dispatching a message to a local endpoint.
		/// </summary>
		public static readonly Uri LoopbackAddress = new Uri("default://loopback/");

		/// <summary>
		/// Represents the address used for dispatching a message to the dead or expired letter queue
		/// </summary>
		public static readonly Uri DeadLetterAddress = new Uri("default://dead-letter-queue/");

		/// <summary>
		/// Represents the address used for dispatching a message to the unhandled message queue
		/// </summary>
		public static readonly Uri UnhandledMessageAddress = new Uri("default://unhandled-message-queue/");

		/// <summary>
		/// Represents the address used for dispatching a message to the unroutable message queue
		/// </summary>
		public static readonly Uri UnroutableMessageAddress = new Uri("default://unroutable-message-queue/");

		/// <summary>
		/// Gets the message to be dispatched.
		/// </summary>
		public virtual ChannelMessage Message { get; private set; }

		/// <summary>
		/// Gets the collection of recipients to which the message will be sent.
		/// </summary>
		public virtual ICollection<Uri> Recipients { get; private set; }

		/// <summary>
		/// Gets a reference to any temporary state used to better understand the context of the dispatch while in the current application process space.
		/// </summary>
		public virtual object State { get; private set; }

		/// <summary>
		/// Initializes a new instance of the ChannelEnvelope class.
		/// </summary>
		/// <param name="message">The message to be dispatched</param>
		/// <param name="recipients">The collection of recipients to which the message will be sent</param>
		/// <param name="state">Any optional and temporary state used to better understand the context of the dispatch while in the current application process space.</param>
		public ChannelEnvelope(ChannelMessage message, IEnumerable<Uri> recipients, object state = null)
			: this()
		{
			if (message == null)
			{
			    throw new ArgumentNullException(nameof(message));
			}

		    if (recipients == null)
		    {
		        throw new ArgumentNullException(nameof(recipients));
		    }

		    Message = message;

			var immutable = new ReadOnlyCollection<Uri>(recipients.Where(x => x != null).ToArray());
			Recipients = immutable;

			if (immutable.Count == 0)
			{
			    throw new ArgumentException("No recipients were provided.", nameof(recipients));
			}

		    State = state;
		}

		/// <summary>
		/// Initializes a new instance of the ChannelEnvelope class.
		/// </summary>
		protected ChannelEnvelope()
		{
		}
	}
}