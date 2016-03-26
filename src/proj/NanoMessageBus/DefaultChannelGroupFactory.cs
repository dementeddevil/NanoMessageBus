﻿namespace NanoMessageBus
{
	using System;
	using Logging;

	public class DefaultChannelGroupFactory
	{
		public virtual IChannelGroup Build(IChannelConnector connector, IChannelGroupConfiguration configuration)
		{
			if (connector == null)
				throw new ArgumentNullException(nameof(connector));

			if (configuration == null)
				throw new ArgumentNullException(nameof(configuration));

			if (configuration.Synchronous)
			{
				Log.Debug("Building a synchronous channel group named '{0}'.", configuration.GroupName);
				return new SynchronousChannelGroup(connector, configuration);
			}

			Log.Debug("Building an asynchronous channel group named '{0}'.", configuration.GroupName);
			var workers = new TaskWorkerGroup<IMessagingChannel>(
				configuration.MinWorkers, configuration.MaxWorkers, configuration.MaxDispatchBuffer);
			return new DefaultChannelGroup(connector, configuration, workers);
		}

		private static readonly ILog Log = LogFactory.Build(typeof(DefaultChannelGroupFactory));
	}
}