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
			{
			    throw new ArgumentNullException(nameof(connector));
			}

		    if (configuration == null)
		    {
		        throw new ArgumentNullException(nameof(configuration));
		    }

		    _connector = connector;
			_configuration = configuration;
		}

        ~SynchronousChannelGroup()
		{
			Dispose(false);
		}

		public virtual bool DispatchOnly => _configuration.DispatchOnly;

        public virtual void Initialize()
		{
			ThrowWhenDisposed();

			Log.Info("Initializing channel group '{0}'.", _configuration.GroupName);
			_initialized = true;
		}

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual IMessagingChannel OpenChannel()
		{
			ThrowWhenDisposed();
			ThrowWhenUninitialized();

			Log.Debug("Opening a caller-owned channel for group '{0}'.", _configuration.GroupName);
			return _connector.Connect(_configuration.GroupName);
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
			if (!_disposed)
			{
			    return;
			}

		    Log.Warn("The channel group has been disposed.");
			throw new ObjectDisposedException(typeof(DefaultChannelGroup).Name);
		}

		protected virtual void ThrowWhenUninitialized()
		{
			if (_initialized)
			{
			    return;
			}

		    Log.Warn("The channel group has not been initialized.");
			throw new InvalidOperationException("The channel group has not been initialized.");
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposing || _disposed)
			{
			    return;
			}

		    Log.Info("Channel group disposed.");
			_disposed = true;
		}
	}
}