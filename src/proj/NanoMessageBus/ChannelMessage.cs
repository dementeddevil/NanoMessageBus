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
			get { return this._messageId; }
		}

		/// <summary>
		/// Gets the value which attaches the message to a larger conversation.
		/// </summary>
		public virtual Guid CorrelationId
		{
			get { return this._correlationId; }
		}

		/// <summary>
		/// Gets the address to which all replies should be directed.
		/// </summary>
		public virtual Uri ReturnAddress
		{
			get { return this._returnAddress; }
		}

		/// <summary>
		/// Gets the message headers which contain additional metadata about the contained messages.
		/// </summary>
		public virtual IDictionary<string, string> Headers
		{
			get { return this._headers; }
		}

		/// <summary>
		/// Gets the collection of contained messages.
		/// </summary>
		public virtual IList<object> Messages
		{
			get { return this._immutable; }
		}

		/// <summary>
		/// Gets or sets a reference to the active logical message currently being handled.
		/// </summary>
		[IgnoreDataMember, XmlIgnore, SoapIgnore]
		public virtual object ActiveMessage { get; set; }

		/// <summary>
		/// Gets a value which indicates the index of the logical message currently being handled.
		/// </summary>
		[IgnoreDataMember, XmlIgnore, SoapIgnore]
		public virtual int ActiveIndex { get; private set; }

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
		/// Sets the current active message to the next available message, if any, and increments the active index.
		/// </summary>
		/// <returns>If successful, returns true; otherwise false.</returns>
		public virtual bool MoveNext()
		{
			if (++this.ActiveIndex >= this._messages.Count)
			{
				this.Reset();
				return false;
			}

			this.ActiveMessage = this._messages[this.ActiveIndex];
			return true;
		}
		
		public virtual void Reset()
		{
			this.ActiveIndex = Inactive;
			this.ActiveMessage = null;
		}

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
			this._messageId = messageId;
			this._correlationId = correlationId;
			this._returnAddress = returnAddress;
			this._headers = headers ?? new Dictionary<string, string>();
			this._messages = (messages ?? new object[0]).Where(x => x != null).ToArray();
			this._immutable = new ReadOnlyCollection<object>(this._messages);

			this.ActiveIndex = Inactive;
		}

		/// <summary>
		/// Initializes a new instance of the ChannelMessage class.
		/// </summary>
		protected ChannelMessage()
		{
		}

		[DataMember(Order = 1, EmitDefaultValue = false, IsRequired = false, Name = "id")]
		private readonly Guid _messageId;
		[DataMember(Order = 2, EmitDefaultValue = false, IsRequired = false, Name = "correlation")]
		private readonly Guid _correlationId;
		[DataMember(Order = 3, EmitDefaultValue = false, IsRequired = false, Name = "sender")]
		private readonly Uri _returnAddress;
		[DataMember(Order = 4, EmitDefaultValue = false, IsRequired = false, Name = "headers")]
		private readonly IDictionary<string, string> _headers;
		[DataMember(Order = 5, EmitDefaultValue = false, IsRequired = false, Name = "payload")]
		private readonly IList<object> _messages;

		[NonSerialized, IgnoreDataMember, XmlIgnore, SoapIgnore]
		private readonly IList<object> _immutable;

		private const int Inactive = -1;
	}
}