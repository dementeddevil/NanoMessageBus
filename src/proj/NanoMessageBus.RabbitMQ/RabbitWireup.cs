namespace NanoMessageBus.RabbitMQ
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class RabbitWireup
	{
		public virtual RabbitWireup ConnectTo(params Uri[] hosts)
		{
			return this.ConnectTo((IEnumerable<Uri>)hosts);
		}
		public virtual RabbitWireup ConnectTo(IEnumerable<Uri> hosts)
		{
			this.Hosts = (hosts ?? new Uri[0]).ToArray();
			if (this.Hosts.Count == 0)
				throw new ArgumentException("No hosts specified.", "hosts");

			return this;
		}
		public virtual RabbitWireup ConnectAnonymouslyToLocalhost()
		{
			this.Hosts.Clear();
			this.Hosts.Add(null);
			return this;
		}

		public virtual RabbitWireup InitializeWith(params Action<object>[] uponConnection)
		{
			return this.InitializeWith((IEnumerable<Action<object>>)uponConnection);
		}
		public virtual RabbitWireup InitializeWith(IEnumerable<Action<object>> uponConnection)
		{
			this.Initializers = (uponConnection ?? new Action<object>[0]).Where(x => x != null).ToArray();
			return this;
		}

		public virtual RabbitWireup AcknowledgeMessages()
		{
			if (this.TransactionType == RabbitTransactionType.None)
				this.TransactionType = RabbitTransactionType.Acknowledge;

			return this;
		}
		public virtual RabbitWireup UseTransactions()
		{
			this.TransactionType = RabbitTransactionType.Full;
			return this;
		}

		public virtual RabbitWireup ListenTo(string queueName)
		{
			queueName = (queueName ?? string.Empty).Trim();
			if (string.IsNullOrEmpty(queueName))
				throw new ArgumentNullException("queueName");

			this.QueueName = queueName;

			return this;
		}

		public virtual RabbitWireup MessagesPerChannel(int prefetchCount)
		{
			if (prefetchCount <= 0)
				throw new ArgumentException("The buffer size must be positive.", "prefetchCount");

			if (prefetchCount > ushort.MaxValue)
				prefetchCount = ushort.MaxValue;

			this.PrefetchCount = prefetchCount;
			return this;
		}

		public virtual RabbitConnector OpenConnection()
		{
			// ensure hosts and queue name have been properly set
			this.ConnectTo(this.Hosts)
				.ListenTo(this.QueueName);

			return this.Current ?? (this.Current = new RabbitConnector(this));
		}

		public RabbitWireup()
		{
			this.Hosts = new LinkedList<Uri>();
		}

		internal ICollection<Uri> Hosts { get; private set; }
		internal string QueueName { get; private set; }
		internal ICollection<Action<object>> Initializers { get; private set; }
		internal RabbitTransactionType TransactionType { get; private set; }
		internal int PrefetchCount { get; private set; }
		internal RabbitConnector Current { get; private set; }
	}
}