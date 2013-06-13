namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;

	public class AsynchronousMessenger : IMessenger
	{
		public virtual void Dispatch(object message, IDictionary<string, string> headers = null, object state = null)
		{
			this.channelGroup.BeginDispatch(x =>
			{
				var dispatch = x.WithMessage(message);

				if (headers != null)
					dispatch = dispatch.WithHeaders(headers);

				if (state != null)
					dispatch = dispatch.WithState(state);

				dispatch
					.Send()
					.Commit();
			});
		}
		public virtual void Commit()
		{
			// no-op
		}

		public AsynchronousMessenger(IChannelGroup channelGroup)
		{
			this.channelGroup = channelGroup;
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			// no op
		}

		private readonly IChannelGroup channelGroup;
	}
}