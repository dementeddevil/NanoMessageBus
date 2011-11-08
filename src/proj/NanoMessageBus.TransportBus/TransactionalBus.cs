namespace NanoMessageBus
{
	using System;
	using Core;

	public class TransactionalBus : ISendMessages, IPublishMessages
	{
		private readonly IHandleUnitOfWork unitOfWork;
		private readonly MessageBus inner;

		public TransactionalBus(IHandleUnitOfWork unitOfWork, MessageBus inner)
		{
			// Null UoW?
			this.unitOfWork = unitOfWork;
			this.inner = inner;
		}

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
	}
}