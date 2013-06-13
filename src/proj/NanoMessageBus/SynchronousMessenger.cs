namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;

	public class SynchronousMessenger : IMessenger
	{
		public virtual void Dispatch(object message, IDictionary<string, string> headers = null, object state = null)
		{
			var dispatch = this.channel.PrepareDispatch(message);

			if (headers != null)
				dispatch = dispatch.WithHeaders(headers);

			if (state != null)
				dispatch = dispatch.WithState(state);

			dispatch.Send();
		}
		public virtual void Commit()
		{
			this.channel.CurrentTransaction.Commit();
		}

		public SynchronousMessenger(IMessagingChannel channel)
		{
			this.channel = channel;
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
				this.channel.Dispose();
		}

		private readonly IMessagingChannel channel;
	}
}