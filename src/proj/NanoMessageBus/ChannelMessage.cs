namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	/// <summary>
	/// Represents an atomic unit of communication--a message or communiqué--that can or has been transported over
	/// a communication medium, which holds both the messages and metadata about those messages.
	/// </summary>
	[DataContract, Serializable]
	public class ChannelMessage
	{
		/// <summary>
		/// Gets the value which uniquely identifies the message.
		/// </summary>
		public virtual Guid MessageId
		{
			get { return this.messageId; }
		}

		/// <summary>
		/// Gets the value which attaches the message to a larger conversation.
		/// </summary>
		public virtual Guid CorrelationId
		{
			get { return this.correlationId; }
		}

		/// <summary>
		/// Gets the address to which all replies should be directed.
		/// </summary>
		public virtual Uri ReturnAddress
		{
			get { return this.returnAddress; }
		}

		/// <summary>
		/// Gets the message headers which contain additional metadata about the contained messages.
		/// </summary>
		public virtual IDictionary<string, string> Headers
		{
			get { return this.headers; }
		}

		/// <summary>
		/// Gets the collection of contained messages.
		/// </summary>
		public virtual IList<object> Messages
		{
			get { return this.immutable; }
		}

		/// <summary>
		/// Gets or sets the maximum amount of time the message will live prior to successful receipt.
		/// </summary>
		[IgnoreDataMember, XmlIgnore, SoapIgnore]
		public virtual DateTime Expiration { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the message is durably stored.
		/// </summary>
		[IgnoreDataMember, XmlIgnore, SoapIgnore]
		public virtual bool Persistent { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the point in time at which the message was dispatched.
		/// </summary>
		[IgnoreDataMember, XmlIgnore, SoapIgnore]
		public virtual DateTime Dispatched { get; set; }

		/// <summary>
		/// Initializes a new instance of the ChannelMessage class.
		/// </summary>
		/// <param name="messageId">The value which uniquely identifies the message.</param>
		/// <param name="correlationId">The value which attaches the message to a larger conversation.</param>
		/// <param name="returnAddress">The address to which all replies should be directed.</param>
		/// <param name="headers">The message headers which contain additional metadata about the contained messages.</param>
		/// <param name="messages">The collection of contained messages.</param>
		public ChannelMessage(
			Guid messageId,
			Guid correlationId,
			Uri returnAddress,
			IDictionary<string, string> headers,
			IEnumerable<object> messages)
		{
			this.messageId = messageId;
			this.correlationId = correlationId;
			this.returnAddress = returnAddress;
			this.headers = headers ?? new Dictionary<string, string>();
			this.messages = (messages ?? new object[0]).Where(x => x != null).ToArray();
			this.immutable = new ReadOnlyCollection<object>(this.messages);
		}

		/// <summary>
		/// Initializes a new instance of the ChannelMessage class.
		/// </summary>
		protected ChannelMessage()
		{
		}

		[DataMember(Order = 1, EmitDefaultValue = false, IsRequired = false, Name = "id")]
		private readonly Guid messageId;
		[DataMember(Order = 2, EmitDefaultValue = false, IsRequired = false, Name = "correlation")]
		private readonly Guid correlationId;
		[DataMember(Order = 3, EmitDefaultValue = false, IsRequired = false, Name = "sender")]
		private readonly Uri returnAddress;
		[DataMember(Order = 4, EmitDefaultValue = false, IsRequired = false, Name = "headers")]
		private readonly IDictionary<string, string> headers;
		[DataMember(Order = 5, EmitDefaultValue = false, IsRequired = false, Name = "payload")]
		private readonly IList<object> messages;

		[NonSerialized, IgnoreDataMember, XmlIgnore, SoapIgnore]
		private readonly IList<object> immutable;
	}
}