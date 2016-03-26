namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;

	public class SynchronousMessenger : IMessenger
	{
		public virtual void Dispatch(object message, IDictionary<string, string> headers = null, object state = null)
		{
			var dispatch = _channel.PrepareDispatch(message);

			if (headers != null)
			{
			    dispatch = dispatch.WithHeaders(headers);
			}

		    if (state != null)
		    {
		        dispatch = dispatch.WithState(state);
		    }

		    dispatch.Send();
		}
		public virtual void Commit()
		{
			_channel.CurrentTransaction.Commit();
		}

		public SynchronousMessenger(IMessagingChannel channel)
		{
			_channel = channel;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
			    _channel.Dispose();
			}
		}

		private readonly IMessagingChannel _channel;
	}
}