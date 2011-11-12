namespace NanoMessageBus.RabbitMQ
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class RabbitConnectorOptions
	{
		public virtual RabbitConnectorOptions ConnectTo(params Uri[] hosts)
		{
			return this.ConnectTo((IEnumerable<Uri>)hosts);
		}
		public virtual RabbitConnectorOptions ConnectTo(IEnumerable<Uri> hosts)
		{
			this.Hosts = (hosts ?? new Uri[0]).Where(x => x != null).ToArray();
			if (this.Hosts.Count == 0)
				throw new ArgumentException("No hosts specified.", "hosts");

			return this;
		}

		public virtual RabbitConnectorOptions InitializeWith(params Action<object>[] uponConnection)
		{
			return this.InitializeWith((IEnumerable<Action<object>>)uponConnection);
		}
		public virtual RabbitConnectorOptions InitializeWith(IEnumerable<Action<object>> uponConnection)
		{
			this.Initializers = (uponConnection ?? new Action<object>[0]).Where(x => x != null).ToArray();
			return this;
		}

		public virtual RabbitConnectorOptions AcknowledgeMessages()
		{
			if (this.TransactionType == RabbitTransactionType.None)
				this.TransactionType = RabbitTransactionType.Acknowledge;

			return this;
		}
		public virtual RabbitConnectorOptions UseTransactions()
		{
			this.TransactionType = RabbitTransactionType.Full;
			return this;
		}

		public virtual RabbitConnectorOptions ListenTo(string queueName)
		{
			queueName = (queueName ?? string.Empty).Trim();
			if (string.IsNullOrEmpty(queueName))
				throw new ArgumentNullException("queueName");

			this.QueueName = queueName;

			return this;
		}

		public virtual RabbitConnectorOptions MessagesPerChannel(int prefetchCount)
		{
			if (prefetchCount <= 0)
				throw new ArgumentException("The buffer size must be positive.", "prefetchCount");

			if (prefetchCount > ushort.MaxValue)
				prefetchCount = ushort.MaxValue;

			this.PrefetchCount = prefetchCount;
			return this;
		}

		public virtual RabbitConnector Connect()
		{
			// ensure hosts and queue name have been properly set
			this.ConnectTo(this.Hosts)
				.ListenTo(this.QueueName);

			return this.Current ?? (this.Current = new RabbitConnector(this));
		}

		internal ICollection<Uri> Hosts { get; private set; }
		internal string QueueName { get; private set; }
		internal ICollection<Action<object>> Initializers { get; private set; }
		internal RabbitTransactionType TransactionType { get; private set; }
		internal int PrefetchCount { get; private set; }
		internal RabbitConnector Current { get; private set; }
	}
}