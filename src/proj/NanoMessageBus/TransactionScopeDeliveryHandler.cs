using System;
using System.Threading.Tasks;
using System.Transactions;
using NanoMessageBus.Logging;

namespace NanoMessageBus
{
	public class TransactionScopeDeliveryHandler : IDeliveryHandler
	{
		private static readonly ILog Log = LogFactory.Build(typeof(TransactionScopeDeliveryHandler));
		private readonly IDeliveryHandler _inner;
		private readonly TransactionScopeOption _scopeOption;
		private readonly TransactionOptions _transactionOptions;

		public TransactionScopeDeliveryHandler(IDeliveryHandler inner)
			: this(inner, TransactionScopeOption.Required)
		{
		}
		public TransactionScopeDeliveryHandler(IDeliveryHandler inner, TransactionScopeOption scopeOption)
			: this(inner, scopeOption, new TransactionOptions())
		{
		}

        public TransactionScopeDeliveryHandler(
			IDeliveryHandler inner, TransactionScopeOption scopeOption, TransactionOptions transactionOptions)
		{
			if (inner == null)
			{
			    throw new ArgumentNullException(nameof(inner));
			}

            _inner = inner;
			_scopeOption = scopeOption;
			_transactionOptions = transactionOptions;
		}

		public virtual async Task HandleAsync(IDeliveryContext delivery)
		{
			Log.Debug("Creating new transaction scope associated for delivery.");
			using (var scope = new TransactionScope(_scopeOption, _transactionOptions))
			{
				await _inner.HandleAsync(delivery).ConfigureAwait(false);

				Log.Debug("Committing transaction scope associated with delivery.");
				scope.Complete();
			}
		}
	}
}