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
		public void Verbose(string message, Exception exception)
		{
			this.DebugWindow(Threshold.Verbose, message, exception);
		}

		public virtual void Debug(string message, params object[] values)
		{
			this.DebugWindow(Threshold.Debug, message, values);
		}
		public void Debug(string message, Exception exception)
		{
			this.DebugWindow(Threshold.Debug, message, exception);
		}

		public virtual void Info(string message, params object[] values)
		{
			this.TraceWindow(Threshold.Info, message, values);
		}
		public void Info(string message, Exception exception)
		{
			this.TraceWindow(Threshold.Info, message, exception);
		}

		public virtual void Warn(string message, params object[] values)
		{
			this.TraceWindow(Threshold.Warn, message, values);
		}
		public void Warn(string message, Exception exception)
		{
			this.TraceWindow(Threshold.Warn, message, exception);
		}

		public virtual void Error(string message, params object[] values)
		{
			this.TraceWindow(Threshold.Error, message, values);
		}
		public void Error(string message, Exception exception)
		{
			this.TraceWindow(Threshold.Error, message, exception);
		}

		public virtual void Fatal(string message, params object[] values)
		{
			this.TraceWindow(Threshold.Fatal, message, values);
		}
		public void Fatal(string message, Exception exception)
		{
			this.TraceWindow(Threshold.Fatal, message, exception);
		}

		protected virtual void DebugWindow(Threshold severity, string message, params object[] values)
		{
			if (severity < this._threshold)
				return;

			lock (Sync)
				System.Diagnostics.Debug.WriteLine(severity, message.FormatMessage(this._typeToLog, values));
		}
		protected virtual void TraceWindow(Threshold severity, string message, params object[] values)
		{
			if (severity < this._threshold)
				return;

			lock (Sync)
				Trace.WriteLine(severity, message.FormatMessage(this._typeToLog, values));
		}

		public TraceLogger(Type typeToLog, Threshold threshold = Threshold.Info)
		{
			this._typeToLog = typeToLog;
			this._threshold = threshold;
		}

		private static readonly object Sync = new object();
		private readonly Type _typeToLog;
		private readonly Threshold _threshold;
	}
}