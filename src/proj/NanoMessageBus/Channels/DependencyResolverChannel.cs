using System.Threading.Tasks;

namespace NanoMessageBus.Channels
{
	using System;
	using Logging;

	public class DependencyResolverChannel : IMessagingChannel
	{
		public virtual bool Active => CurrentContext.Active;

        public virtual ChannelMessage CurrentMessage => CurrentContext.CurrentMessage;

        public virtual IChannelTransaction CurrentTransaction => CurrentContext.CurrentTransaction;

	    public virtual IChannelGroupConfiguration CurrentConfiguration => CurrentContext.CurrentConfiguration;

	    public virtual IDependencyResolver CurrentResolver => _currentResolver ?? _resolver;

	    protected virtual IDeliveryContext CurrentContext => _currentContext ?? _channel;

	    public virtual IDispatchContext PrepareDispatch(object message = null, IMessagingChannel actual = null)
		{
			Log.Debug("Preparing a dispatch");
			return CurrentContext.PrepareDispatch(message, actual ?? this);
		}
		public virtual Task SendAsync(ChannelEnvelope envelope)
		{
			Log.Verbose("Sending envelope '{0}' through the underlying channel.", envelope.MessageId());
			return _channel.SendAsync(envelope);
		}

		public virtual Task ShutdownAsync()
		{
			return _channel.ShutdownAsync();
		}

		public virtual Task ReceiveAsync(Func<IDeliveryContext, Task> callback)
		{
			return _channel.ReceiveAsync(context => ReceiveAsync(context, callback));
		}

        protected virtual async Task ReceiveAsync(IDeliveryContext context, Func<IDeliveryContext, Task> callback)
		{
			try
			{
				Log.Verbose("Delivery received, attempting to create nested resolver.");
				_currentContext = context;
				_currentResolver = _resolver.CreateNestedResolver();
				await callback(this).ConfigureAwait(false);
			}
			finally
			{
				Log.Verbose("Delivery completed, disposing nested resolver.");
				_currentResolver.TryDispose();
				_currentResolver = null;
				_currentContext = null;
			}
		}

		public DependencyResolverChannel(IMessagingChannel channel, IDependencyResolver resolver)
		{
			if (channel == null)
			{
			    throw new ArgumentNullException(nameof(channel));
			}

		    if (resolver == null)
		    {
		        throw new ArgumentNullException(nameof(resolver));
		    }

		    _channel = channel;
			_resolver = resolver;
		}
		~DependencyResolverChannel()
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
			if (!disposing)
			{
			    return;
			}

		    Log.Verbose("Disposing the underlying channel and resolver.");
			_channel.TryDispose();
			_resolver.TryDispose();
		}

		private static readonly ILog Log = LogFactory.Build(typeof(DependencyResolverChannel));
		private readonly IMessagingChannel _channel;
		private readonly IDependencyResolver _resolver;
		private IDependencyResolver _currentResolver;
		private IDeliveryContext _currentContext;
	}
}