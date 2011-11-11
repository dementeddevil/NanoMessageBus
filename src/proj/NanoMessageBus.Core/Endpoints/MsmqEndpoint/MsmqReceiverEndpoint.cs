namespace NanoMessageBus.Endpoints.MsmqEndpoint
{
	using System;
	using System.Messaging;
	using System.Runtime.Serialization;
	using Logging;
	using Serialization;

	public class MsmqReceiverEndpoint : IReceiveFromEndpoints
	{
		public virtual EnvelopeMessage Receive()
		{
			var message = this.DequeueMessage();
			if (message == null)
				return null;

			Log.Info(Diagnostics.MessageReceived, message.BodyStream.Length, this.inputQueue.Address);

			using (message)
			using (message.BodyStream)
				return this.Deserialize(message);
		}
		private Message DequeueMessage()
		{
			try
			{
				return this.inputQueue.Receive(Timeout);
			}
			catch (MessageQueueException e)
			{
				if (e.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
					return this.NoMessageAvailable();

				if (e.MessageQueueErrorCode == MessageQueueErrorCode.AccessDenied)
					Log.Fatal(Diagnostics.AccessDenied, this.inputQueue.Address);

				throw new EndpointException(e.Message, e);
			}
		}
		private Message NoMessageAvailable()
		{
			Log.Verbose(Diagnostics.NoMessageAvailable, this.inputQueue.Address);
			return null;
		}
		private EnvelopeMessage Deserialize(Message message)
		{
			try
			{
				return (EnvelopeMessage)this.serializer.Deserialize(message.BodyStream);
			}
			catch (SerializationException e)
			{
				Log.Error(Diagnostics.UnableToDeserializeMessage, e);
				this.ForwardToPoisonMessageQueue(message, e);
				return null;
			}
		}
		private void ForwardToPoisonMessageQueue(Message message, Exception exception)
		{
			message.Extension = exception.Serialize();
			this.poisonQueue.Send(message);
		}

		public MsmqReceiverEndpoint(
			MsmqConnector inputQueue, MsmqConnector poisonQueue, ISerializer serializer)
		{
			this.inputQueue = inputQueue;
			this.poisonQueue = poisonQueue;
			this.serializer = serializer;
		}
		~MsmqReceiverEndpoint()
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
			if (this.disposed || !disposing)
				return;

			this.disposed = true;
			this.inputQueue.Dispose();
		}

		private static readonly ILog Log = LogFactory.BuildLogger(typeof(MsmqReceiverEndpoint));
		private static readonly TimeSpan Timeout = 1000.Milliseconds();
		private readonly MsmqConnector inputQueue;
		private readonly MsmqConnector poisonQueue;
		private readonly ISerializer serializer;
		private bool disposed;
	}
}