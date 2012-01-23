namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;

	public class DefaultDispatchContext : IDispatchContext
	{
		public IDispatchContext WithMessage(object message)
		{
			return null;
		}
		public IDispatchContext WithMessages(params object[] messages)
		{
			return null;
		}

		public IDispatchContext WithCorrelationId(Guid correlationId)
		{
			return null;
		}

		public IDispatchContext WithHeader(string key, string value = null)
		{
			return null;
		}
		public IDispatchContext WithHeaders(IDictionary<string, string> headers)
		{
			return null;
		}

		public IDispatchContext WithRecipient(Uri recipient)
		{
			return null;
		}

		public void Send()
		{
		}
		public void Publish()
		{
		}
		public void Reply()
		{
		}

		public DefaultDispatchContext(IDeliveryContext delivery, IDispatchTable dispatchTable)
		{
			this.delivery = delivery;
			this.dispatchTable = dispatchTable;
		}

		private readonly IDeliveryContext delivery;
		private readonly IDispatchTable dispatchTable;
	}
}