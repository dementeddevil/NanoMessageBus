namespace NanoMessageBus.RabbitMQ
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Threading;
	using Endpoints;
	using global::RabbitMQ.Client;
	using global::RabbitMQ.Client.Exceptions;

	// this is singleton-scoped and opens up connections to RabbitChannel as necessary
	public class RabbitConnector : IDisposable
	{
		public RabbitChannel Current
		{
			get { return this.EstablishChannel(); }
		}
		private RabbitChannel EstablishChannel()
		{
			var storage = new ThreadStorage();
			var channel = storage[ThreadKey] as RabbitChannel;
			if (channel != null)
				return channel;

			this.ThrowWhenDisposed();
			channel = new RabbitChannel(this.EstablishConnection, this.options);
			storage[ThreadKey] = channel;

			lock (this.locker)
				this.active.Add(channel);

			return channel;
		}

		public object EstablishConnection()
		{
			this.ThrowWhenDisposed();

			lock (this.locker)
			{
				// TODO: while a connection is still good, return it
				// when it fails, close it so that it's not removed
				// perhaps this is where even a RabbitChannel can signal us if the model is closing?
				return this.current ?? (this.current = this.TryConnect());
			}
		}
		private void ThrowWhenDisposed()
		{
			if (Interlocked.Read(ref this.disposed) > 1)
				throw new ObjectDisposedException("RabbitConnector", "The connector has already been disposed.");
		}
		private IConnection TryConnect()
		{
			if (this.current != null && this.current.IsOpen)
				return this.current;

			var connection = this.options.Hosts.Select(TryConnect).FirstOrDefault();
			if (connection == null)
				throw new EndpointUnavailableException();

			// TODO: re-establish the connection if the shutdown was unexpected
			connection.ConnectionShutdown += (conn, reason) => { };

			return connection;
		}
		private static IConnection TryConnect(Uri host)
		{
			try
			{
				if (host == null)
					return new ConnectionFactory().CreateConnection();

				return new ConnectionFactory { Address = host.ToString() }.CreateConnection();
			}
			catch (PossibleAuthenticationFailureException)
			{
				return null;
			}
			catch (ProtocolViolationException)
			{
				return null;
			}
			catch (ChannelAllocationException)
			{
				return null;
			}
			catch (OperationInterruptedException)
			{
				return null;
			}
		}

		public RabbitConnector(RabbitWireup options)
		{
			// this class is responsible for opening *and maintaining* a connection to any
			// of the Rabbit brokers specified and for performing any initialization
			// necessary, e.g. declaring queues and exchanges
			// and for providing a way for the suboordinate channels to get a reference to the
			// underlying connection should the connection fail
			this.options = options;
		}
		~RabbitConnector()
		{
			this.Dispose(false);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing || Interlocked.Increment(ref this.disposed) > 1)
				return; // already disposed

			lock (this.locker)
			{
				// TODO: figure out how to signal channels such that they dispose themselves and remove themselves from TLS.
				foreach (var channel in this.active)
					this.current.Dispose(); // TODO: not multi-thread safe

				this.active.Clear();

				// TODO: abort channel with timeout?
				if (this.current != null)
					this.current.Dispose();
				this.current = null;
			}
		}

		internal const string ThreadKey = "RabbitConnector";
		private readonly object locker = new object();
		private readonly ICollection<RabbitChannel> active = new LinkedList<RabbitChannel>();
		private readonly RabbitWireup options;
		private IConnection current;
		private long disposed;
	}
}