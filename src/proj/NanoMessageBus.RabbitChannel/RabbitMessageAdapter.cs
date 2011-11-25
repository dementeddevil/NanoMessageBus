namespace NanoMessageBus.RabbitChannel
{
	using System;
	using System.Collections.Generic;
	using RabbitMQ.Client.Events;
	using Serialization;

	public class RabbitMessageAdapter
	{
		public virtual ChannelMessage Build(BasicDeliverEventArgs message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			lock (this.cache)
				this.cache[message] = null; // TODO

			// TODO: track message in thread-safe collection
			return null; // TODO
		}
		public virtual BasicDeliverEventArgs Build(ChannelMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			return null; // TODO
		}

		public virtual void AppendException(BasicDeliverEventArgs message, Exception exception)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			if (exception == null)
				throw new ArgumentNullException("exception");

			this.AppendException(message, exception, 0);
		}
		protected virtual void AppendException(BasicDeliverEventArgs message, Exception exception, int depth)
		{
			if (exception == null)
				return;

			message.SetHeader(HeaderFormat.FormatWith(depth, "type"), exception.GetType());
			message.SetHeader(HeaderFormat.FormatWith(depth, "message"), exception.Message);
			message.SetHeader(HeaderFormat.FormatWith(depth, "stacktrace"), exception.StackTrace ?? string.Empty);

			this.AppendException(message, exception.InnerException, depth + 1);
		}

		public virtual bool PurgeFromCache(BasicDeliverEventArgs message)
		{
			lock (this.cache)
				return this.cache.Remove(message);
		}

		public RabbitMessageAdapter(ISerializer serializer) : this()
		{
			this.serializer = serializer;
		}
		protected RabbitMessageAdapter()
		{
		}

		private const string HeaderFormat = "x-exception{0}-{1}";
		private readonly IDictionary<BasicDeliverEventArgs, ChannelMessage> cache =
			new Dictionary<BasicDeliverEventArgs, ChannelMessage>();
		private readonly ISerializer serializer;
	}
}