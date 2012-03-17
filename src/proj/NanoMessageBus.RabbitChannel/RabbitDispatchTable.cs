namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections.Generic;

	public class RabbitDispatchTable : IDispatchTable
	{
		public virtual ICollection<Uri> this[Type messageType]
		{
			get
			{
				if (messageType == null)
					throw new ArgumentNullException("messageType");

				return new[] { new Uri("fanout://" + messageType.FullName.NormalizeName(), UriKind.Absolute) };
			}
		}
		public virtual void AddSubscriber(Uri subscriber, Type messageType, DateTime expiration)
		{
			// no op
		}
		public virtual void AddRecipient(Uri recipient, Type messageType)
		{
			// no op
		}
		public virtual void Remove(Uri subscriber, Type messageType)
		{
			// no op
		}
	}
}