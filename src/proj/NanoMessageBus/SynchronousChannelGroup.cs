namespace NanoMessageBus
{
	using System;
	using Logging;

	public class SynchronousChannelGroup : IChannelGroup
	{
		public bool DispatchOnly
		{
			get { return this.configuration.DispatchOnly; }
		}

		public void Initialize()
		{
			this.ThrowWhenDisposed();

			Log.Info("Initializing channel group '{0}'.", this.configuration.GroupName);
			this.initialized = true;
		}
		public IMessagingChannel OpenChannel()
		{
			this.ThrowWhenDisposed();
			this.ThrowWhenUninitialized();

			Log.Debug("Opening a caller-owned channel for group '{0}'.", this.configuration.GroupName);
			return this.connector.Connect(this.configuration.GroupName);
		}

		public void BeginReceive(Action<IDeliveryContext> callback)
		{
			throw new NotSupportedException();
		}
		public bool BeginDispatch(Action<IDispatchContext> callback)
		{
			throw new NotSupportedException();
		}

		protected virtual void ThrowWhenDisposed()
		{
			if (!this.disposed)
				return;

			Log.Warn("The channel group has been disposed.");
			throw new ObjectDisposedException(typeof(DefaultChannelGroup).Name);
		}
		protected virtual void ThrowWhenUninitialized()
		{
			if (this.initialized)
				return;

			Log.Warn("The channel group has not been initialized.");
			throw new InvalidOperationException("The channel group has not been initialized.");
		}

		public SynchronousChannelGroup(IChannelConnector connector, IChannelGroupConfiguration configuration)
		{
			if (connector == null)
				throw new ArgumentNullException("connector");

			if (configuration == null)
				throw new ArgumentNullException("configuration");

			this.connector = connector;
			this.configuration = configuration;
		}
		~SynchronousChannelGroup()
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
			if (!disposing || this.disposed)
				return;

			Log.Info("Channel group disposed.");
			this.disposed = true;
		}

		private static readonly ILog Log = LogFactory.Build(typeof(SynchronousChannelGroup));
		private readonly IChannelConnector connector;
		private readonly IChannelGroupConfiguration configuration;
		private bool initialized;
		private bool disposed;
	}
}