namespace NanoMessageBus.Logging
{
	using System;
	using System.Diagnostics;

	public class TraceLogger : ILog
	{
		public virtual void Verbose(string message, params object[] values)
		{
			this.DebugWindow(Threshold.Verbose, message, values);
		}
		public virtual void Debug(string message, params object[] values)
		{
			this.DebugWindow(Threshold.Debug, message, values);
		}
		public virtual void Info(string message, params object[] values)
		{
			this.TraceWindow(Threshold.Info, message, values);
		}
		public virtual void Warn(string message, params object[] values)
		{
			this.TraceWindow(Threshold.Warn, message, values);
		}
		public virtual void Error(string message, params object[] values)
		{
			this.TraceWindow(Threshold.Error, message, values);
		}
		public virtual void Fatal(string message, params object[] values)
		{
			this.TraceWindow(Threshold.Fatal, message, values);
		}
		protected virtual void DebugWindow(Threshold category, string message, params object[] values)
		{
			if (category < this.threshold)
				return;

			lock (Sync)
				System.Diagnostics.Debug.WriteLine(category, message.FormatMessage(this.typeToLog, values));
		}
		protected virtual void TraceWindow(Threshold category, string message, params object[] values)
		{
			if (category < this.threshold)
				return;

			lock (Sync)
				Trace.WriteLine(category, message.FormatMessage(this.typeToLog, values));
		}

		public TraceLogger(Type typeToLog, Threshold threshold = Threshold.Info)
		{
			this.typeToLog = typeToLog;
			this.threshold = threshold;
		}

		private static readonly object Sync = new object();
		private readonly Type typeToLog;
		private readonly Threshold threshold;
	}
}