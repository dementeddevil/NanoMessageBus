namespace NanoMessageBus
{
	using System;
	using System.Transactions;
	using Logging;

	public class TransactionScopeDeliveryHandler : IDeliveryHandler
	{
		public virtual void Handle(IDeliveryContext delivery)
		{
			Log.Debug("Creating new transaction scope associated for delivery.");
			using (var scope = new TransactionScope(this.scopeOption, this.transactionOptions))
			{
				this.inner.Handle(delivery);

				Log.Debug("Committing transaction scope associated with delivery.");
				scope.Complete();
			}
		}

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
				throw new ArgumentNullException("inner");

			this.inner = inner;
			this.scopeOption = scopeOption;
			this.transactionOptions = transactionOptions;
		}

		private readonly IDeliveryHandler inner;
		private readonly TransactionScopeOption scopeOption;
		private readonly TransactionOptions transactionOptions;
		private static readonly ILog Log = LogFactory.Build(typeof(TransactionScopeDeliveryHandler));
	}
}