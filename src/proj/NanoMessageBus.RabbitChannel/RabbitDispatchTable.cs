namespace NanoMessageBus.RabbitChannel
{
	using System;
	using System.Collections.Generic;
	using Logging;

	public class RabbitDispatchTable : IDispatchTable
	{
		public ICollection<Uri> this[Type messageType]
		{
			get
			{
				if (messageType == null)
					throw new ArgumentNullException("messageType");

				return new[] { new Uri("fanout://" + messageType.FullName.AsLower(), UriKind.Absolute) };
			}
		}
		public void AddSubscriber(Uri subscriber, Type messageType, DateTime expiration)
		{
			// no op
		}
		public void AddRecipient(Uri recipient, Type messageType)
		{
			// no op
		}
		public void Remove(Uri subscriber, Type messageType)
		{
			// no op
		}

		private static readonly ILog Log = LogFactory.Build(typeof(RabbitDispatchTable));
	}
}