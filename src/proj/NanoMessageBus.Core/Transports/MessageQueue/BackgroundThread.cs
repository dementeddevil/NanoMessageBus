namespace NanoMessageBus.Transports.MessageQueue
{
	using System;
	using System.Threading;

	public class BackgroundThread : IThread
	{
		public virtual bool IsAlive
		{
			get { return this.thread.IsAlive; }
		}
		public virtual string Name
		{
			get { return this.thread.Name; }
		}
		public virtual void Start()
		{
			this.thread.Start();
		}
		public virtual void Abort()
		{
			this.thread.Abort();
		}

		public BackgroundThread(Action startAction)
			: this(((ThreadStart)(() => startAction())))
		{
		}
		public BackgroundThread(ThreadStart startAction)
		{
			this.thread = new Thread(startAction) { IsBackground = true };
			this.thread.Name = Diagnostics.WorkerThreadName.FormatWith(this.thread.ManagedThreadId);
		}

		private readonly Thread thread;
	}
}