namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;

	public class ResolverConnector : IChannelConnector
	{
		public IMessagingChannel Connect(string channelGroup)
		{
			// TODO: create child container using new resolver channel
			return this.connector.Connect(channelGroup);
		}
		public ConnectionState CurrentState
		{
			get { return this.connector.CurrentState; }
		}
		public IEnumerable<IChannelGroupConfiguration> ChannelGroups
		{
			get { return this.connector.ChannelGroups; }
		}

		public ResolverConnector(IChannelConnector connector)
		{
			this.connector = connector;
		}
		public void Dispose()
		{
			this.connector.Dispose();
		}

		private readonly IChannelConnector connector;

		private class ResolverChannel : IMessagingChannel
		{
			public IResolver Resolver
			{
				get { return this.nestedResolver ?? this.resolver; }
			}
			public ChannelMessage CurrentMessage
			{
				get { return this.channel.CurrentMessage; }
			}
			public IChannelTransaction CurrentTransaction
			{
				get { return this.channel.CurrentTransaction; }
			}
			public void Send(ChannelEnvelope envelope)
			{
				this.channel.Send(envelope);
			}
			public void BeginShutdown()
			{
				this.channel.BeginShutdown();
			}
			public void Receive(Action<IDeliveryContext> callback)
			{
				this.channel.Receive(context =>
				{
					try
					{
						this.nestedResolver = this.resolver.CreateNestedResolver();
						callback(this);
					}
					finally
					{
						if (nestedResolver != null)
							this.nestedResolver.Dispose();

						this.nestedResolver = null;
					}
				});
			}

			public ResolverChannel(IMessagingChannel channel, IResolver resolver)
			{
				this.channel = channel;
				this.resolver = resolver;
			}
			public void Dispose()
			{
				try
				{
					this.channel.Dispose();
				}
				finally
				{
					this.resolver.Dispose();
				}
			}

			private readonly IMessagingChannel channel;
			private readonly IResolver resolver;
			private IResolver nestedResolver;
		}
	}
}