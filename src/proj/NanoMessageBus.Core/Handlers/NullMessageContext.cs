namespace NanoMessageBus.Handlers
{
	using System;

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
			get { return new EnvelopeMessage(Guid.Empty, Guid.Empty, this.localAddress, TimeSpan.Zero, false, null, null); }
		}
		public virtual bool ContinueProcessing
		{
			get { return false; }
		}

		public NullMessageContext(Uri localAddress)
		{
			this.localAddress = localAddress;
		}

		private readonly Uri localAddress;
	}
}