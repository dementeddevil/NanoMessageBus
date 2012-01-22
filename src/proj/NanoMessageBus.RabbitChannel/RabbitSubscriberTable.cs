namespace NanoMessageBus.RabbitChannel
{
	using System;
	using System.Collections.Generic;

	public class RabbitSubscriberTable : ISubscriberTable
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
		public void Add(Uri subscriber, Type messageType, DateTime expiration)
		{
			// no op
		}
		public void Remove(Uri subscriber, Type messageType)
		{
			// no op
		}
	}
}