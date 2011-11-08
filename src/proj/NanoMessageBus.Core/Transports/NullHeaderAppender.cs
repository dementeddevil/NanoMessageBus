namespace NanoMessageBus.Transports
{
	public class NullHeaderAppender : IAppendHeaders
	{
		public virtual void AppendHeaders(EnvelopeMessage message)
		{
			// no op
		}
	}
}