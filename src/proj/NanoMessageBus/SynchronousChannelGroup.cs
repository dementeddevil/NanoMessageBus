using System;
using System.Threading.Tasks;
using NanoMessageBus.Logging;

namespace NanoMessageBus
{
    public class SynchronousChannelGroup : IChannelGroup
	{
		private static readonly ILog Log = LogFactory.Build(typeof(SynchronousChannelGroup));
		private readonly IChannelConnector _connector;
		private readonly IChannelGroupConfiguration _configuration;
		private bool _initialized;
		private bool _disposed;

		public SynchronousChannelGroup(IChannelConnector connector, IChannelGroupConfiguration configuration)
		{
			if (connector == null)
				throw new ArgumentNullException(nameof(connector));

			if (configuration == null)
				throw new ArgumentNullException(nameof(configuration));

			this._connector = connector;
			this._configuration = configuration;
		}

        ~SynchronousChannelGroup()
		{
			this.Dispose(false);
		}

		public virtual bool DispatchOnly
		{
			get { return this._configuration.DispatchOnly; }
		}

		public virtual void Initialize()
		{
			this.ThrowWhenDisposed();

			Log.Info("Initializing channel group '{0}'.", this._configuration.GroupName);
			this._initialized = true;
		}

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual IMessagingChannel OpenChannel()
		{
			this.ThrowWhenDisposed();
			this.ThrowWhenUninitialized();

			Log.Debug("Opening a caller-owned channel for group '{0}'.", this._configuration.GroupName);
			return this._connector.Connect(this._configuration.GroupName);
		}

		public virtual void BeginReceive(Func<IDeliveryContext, Task> callback)
		{
			throw new NotSupportedException();
		}

		public virtual bool BeginDispatch(Action<IDispatchContext> callback)
		{
			throw new NotSupportedException();
		}

		protected virtual void ThrowWhenDisposed()
		{
			if (!this._disposed)
				return;

			Log.Warn("The channel group has been disposed.");
			throw new ObjectDisposedException(typeof(DefaultChannelGroup).Name);
		}

		protected virtual void ThrowWhenUninitialized()
		{
			if (this._initialized)
				return;

			Log.Warn("The channel group has not been initialized.");
			throw new InvalidOperationException("The channel group has not been initialized.");
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposing || this._disposed)
				return;

			Log.Info("Channel group disposed.");
			this._disposed = true;
		}
	}
}