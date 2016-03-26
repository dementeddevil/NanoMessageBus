namespace NanoMessageBus.Channels
{
	using System;
	using Logging;

	public class DependencyResolverChannel : IMessagingChannel
	{
		public virtual bool Active
		{
			get { return this.CurrentContext.Active; }
		}
		public virtual ChannelMessage CurrentMessage
		{
			get { return this.CurrentContext.CurrentMessage; }
		}
		public virtual IChannelTransaction CurrentTransaction
		{
			get { return this.CurrentContext.CurrentTransaction; }
		}
		public virtual IChannelGroupConfiguration CurrentConfiguration
		{
			get { return this.CurrentContext.CurrentConfiguration; }
		}
		public virtual IDependencyResolver CurrentResolver
		{
			get { return this._currentResolver ?? this._resolver; }
		}
		protected virtual IDeliveryContext CurrentContext
		{
			get { return this._currentContext ?? this._channel; }
		}

		public virtual IDispatchContext PrepareDispatch(object message = null, IMessagingChannel actual = null)
		{
			Log.Debug("Preparing a dispatch");
			return this.CurrentContext.PrepareDispatch(message, actual ?? this);
		}
		public virtual void Send(ChannelEnvelope envelope)
		{
			Log.Verbose("Sending envelope '{0}' through the underlying channel.", envelope.MessageId());
			this._channel.Send(envelope);
		}

		public virtual void BeginShutdown()
		{
			this._channel.BeginShutdown();
		}
		public virtual void Receive(Action<IDeliveryContext> callback)
		{
			this._channel.Receive(context => this.Receive(context, callback));
		}
		protected virtual void Receive(IDeliveryContext context, Action<IDeliveryContext> callback)
		{
			try
			{
				Log.Verbose("Delivery received, attempting to create nested resolver.");
				this._currentContext = context;
				this._currentResolver = this._resolver.CreateNestedResolver();
				callback(this);
			}
			finally
			{
				Log.Verbose("Delivery completed, disposing nested resolver.");
				this._currentResolver.TryDispose();
				this._currentResolver = null;
				this._currentContext = null;
			}
		}

		public DependencyResolverChannel(IMessagingChannel channel, IDependencyResolver resolver)
		{
			if (channel == null)
				throw new ArgumentNullException(nameof(channel));

			if (resolver == null)
				throw new ArgumentNullException(nameof(resolver));

			this._channel = channel;
			this._resolver = resolver;
		}
		~DependencyResolverChannel()
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
			if (!disposing)
				return;

			Log.Verbose("Disposing the underlying channel and resolver.");
			this._channel.TryDispose();
			this._resolver.TryDispose();
		}

		private static readonly ILog Log = LogFactory.Build(typeof(DependencyResolverChannel));
		private readonly IMessagingChannel _channel;
		private readonly IDependencyResolver _resolver;
		private IDependencyResolver _currentResolver;
		private IDeliveryContext _currentContext;
	}
}