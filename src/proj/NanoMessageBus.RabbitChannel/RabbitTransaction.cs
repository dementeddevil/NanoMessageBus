namespace NanoMessageBus.RabbitChannel
{
	using System;

	public class RabbitTransaction : IChannelTransaction
	{
		public virtual bool Finished
		{
			get { return false; }
		}
		public virtual void Register(Action callback)
		{
		}
		public virtual void Complete()
		{
		}
		public virtual void Rollback()
		{
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
		}
	}
}