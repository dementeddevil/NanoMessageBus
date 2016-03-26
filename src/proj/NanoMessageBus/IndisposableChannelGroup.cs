using System.Threading.Tasks;

namespace NanoMessageBus
{
	using System;

	public class IndisposableChannelGroup : IChannelGroup
	{
		public virtual IChannelGroup Inner => _inner;

	    public virtual bool DispatchOnly => _inner.DispatchOnly;

	    public virtual void Initialize()
		{
			_inner.Initialize();
		}
		public virtual IMessagingChannel OpenChannel()
		{
			return _inner.OpenChannel();
		}
		public virtual void BeginReceive(Func<IDeliveryContext, Task> callback)
		{
			_inner.BeginReceive(callback);
		}
		public virtual bool BeginDispatch(Action<IDispatchContext> callback)
		{
			return _inner.BeginDispatch(callback);
		}
		
		public IndisposableChannelGroup(IChannelGroup inner)
		{
			if (inner == null)
			{
			    throw new ArgumentNullException(nameof(inner));
			}

		    _inner = inner;
		}
		~IndisposableChannelGroup()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			// no op
		}

		private readonly IChannelGroup _inner;
	}
}