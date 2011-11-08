namespace NanoMessageBus.Handlers
{
	using System;
	using System.Collections.Generic;

	public class NullMessageContext : IMessageContext
	{
		public virtual void DeferMessage()
		{
		}
		public virtual void DropMessage()
		{
		}
		public virtual EnvelopeMessage CurrentMessage
		{
			get { return new EnvelopeMessage(Guid.Empty, this.localAddress, TimeSpan.Zero, false, null, null); }
		}
		public virtual bool ContinueProcessing
		{
			get { return false; }
		}

		public virtual IDictionary<string, string> OutgoingHeaders
		{
			get { return this.headers; }
		}

		public NullMessageContext(Uri localAddress)
		{
			this.localAddress = localAddress;
		}

		private readonly Uri localAddress;
		private readonly IDictionary<string, string> headers = new Dictionary<string, string>();
	}
}