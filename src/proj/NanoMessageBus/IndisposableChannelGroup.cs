using System.Threading.Tasks;

namespace NanoMessageBus
{
	using System;

	public class IndisposableChannelGroup : IChannelGroup
	{
		public virtual IChannelGroup Inner
		{
			get { return this._inner; }
		}
		public virtual bool DispatchOnly
		{
			get { return this._inner.DispatchOnly; }
		}

		public virtual void Initialize()
		{
			this._inner.Initialize();
		}
		public virtual IMessagingChannel OpenChannel()
		{
			return this._inner.OpenChannel();
		}
		public virtual void BeginReceive(Func<IDeliveryContext, Task> callback)
		{
			this._inner.BeginReceive(callback);
		}
		public virtual bool BeginDispatch(Action<IDispatchContext> callback)
		{
			return this._inner.BeginDispatch(callback);
		}
		
		public IndisposableChannelGroup(IChannelGroup inner)
		{
			if (inner == null)
				throw new ArgumentNullException(nameof(inner));

			this._inner = inner;
		}
		~IndisposableChannelGroup()
		{
			this.Dispose(false);
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

		private readonly IChannelGroup _inner;
	}
}