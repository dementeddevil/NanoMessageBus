namespace NanoMessageBus.Transports
{
	using System;
	using Handlers;

	public class TransactionalBus : ISendMessages, IPublishMessages
	{
		public virtual void Send(params object[] messages)
		{
			this.Register(() => this.inner.Send(messages));
		}
		public virtual void Reply(params object[] messages)
		{
			this.Register(() => this.inner.Reply(messages));
		}
		public virtual void Publish(params object[] messages)
		{
			this.Register(() => this.inner.Publish(messages));
		}
		private void Register(Action callback)
		{
			if (this.unitOfWork == null)
				callback();
			else
				this.unitOfWork.Register(callback);
		}

		public TransactionalBus(IHandleUnitOfWork unitOfWork, MessageBus inner)
		{
			this.unitOfWork = unitOfWork;
			this.inner = inner;
		}

		private readonly IHandleUnitOfWork unitOfWork;
		private readonly MessageBus inner;
	}
}