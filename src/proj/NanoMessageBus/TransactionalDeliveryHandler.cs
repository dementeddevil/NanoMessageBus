using System.Threading.Tasks;

namespace NanoMessageBus
{
	using System;
	using Logging;

	public class TransactionalDeliveryHandler : IDeliveryHandler
	{
		private static readonly ILog Log = LogFactory.Build(typeof(TransactionalDeliveryHandler));
		private readonly IDeliveryHandler _inner;

        public TransactionalDeliveryHandler(IDeliveryHandler inner)
		{
            if (inner == null)
            {
                throw new ArgumentNullException(nameof(inner));
            }

			_inner = inner;
		}

		public virtual async Task HandleAsync(IDeliveryContext delivery)
		{
		    if (delivery == null)
		    {
		        throw new ArgumentNullException(nameof(delivery));
		    }

			Log.Debug("Channel message delivery received, routing to inner delivery handler.");
			await this._inner.HandleAsync(delivery).ConfigureAwait(false);

			Log.Debug("Committing transaction associated with delivery.");
			delivery.CurrentTransaction.Commit();
		}
	}
}