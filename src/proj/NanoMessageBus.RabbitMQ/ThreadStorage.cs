namespace NanoMessageBus.RabbitMQ
{
	using System;
	using System.Collections;
	using System.Web;

	/// <summary>
	/// Instances of this object can only be used within the context of a single method
	/// and must not be shared (as a member variable)
	/// </summary>
	internal class ThreadStorage
	{
		public object this[string key]
		{
			get { return this.local[key]; }
			set { this.local[key] = value; }
		}
		public ThreadStorage Remove(string key)
		{
			this.local.Remove(key);
			return this;
		}

		public ThreadStorage()
		{
			var context = HttpContext.Current;
			if (context == null)
				this.local = thread = thread ?? new Hashtable();
			else
				this.local = context.Items as Hashtable;
		}

		[ThreadStatic]
		private static Hashtable thread;
		private readonly Hashtable local;
	}
}