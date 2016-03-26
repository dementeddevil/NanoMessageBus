namespace NanoMessageBus.Logging
{
	using System;
	using System.Diagnostics;

	public class TraceLogger : ILog
	{
		public virtual void Verbose(string message, params object[] values)
		{
			DebugWindow(Threshold.Verbose, message, values);
		}
		public void Verbose(string message, Exception exception)
		{
			DebugWindow(Threshold.Verbose, message, exception);
		}

		public virtual void Debug(string message, params object[] values)
		{
			DebugWindow(Threshold.Debug, message, values);
		}
		public void Debug(string message, Exception exception)
		{
			DebugWindow(Threshold.Debug, message, exception);
		}

		public virtual void Info(string message, params object[] values)
		{
			TraceWindow(Threshold.Info, message, values);
		}
		public void Info(string message, Exception exception)
		{
			TraceWindow(Threshold.Info, message, exception);
		}

		public virtual void Warn(string message, params object[] values)
		{
			TraceWindow(Threshold.Warn, message, values);
		}
		public void Warn(string message, Exception exception)
		{
			TraceWindow(Threshold.Warn, message, exception);
		}

		public virtual void Error(string message, params object[] values)
		{
			TraceWindow(Threshold.Error, message, values);
		}
		public void Error(string message, Exception exception)
		{
			TraceWindow(Threshold.Error, message, exception);
		}

		public virtual void Fatal(string message, params object[] values)
		{
			TraceWindow(Threshold.Fatal, message, values);
		}
		public void Fatal(string message, Exception exception)
		{
			TraceWindow(Threshold.Fatal, message, exception);
		}

		protected virtual void DebugWindow(Threshold severity, string message, params object[] values)
		{
			if (severity < _threshold)
			{
			    return;
			}

		    lock (Sync)
		    {
		        System.Diagnostics.Debug.WriteLine(severity, message.FormatMessage(_typeToLog, values));
		    }
		}
		protected virtual void TraceWindow(Threshold severity, string message, params object[] values)
		{
			if (severity < _threshold)
			{
			    return;
			}

		    lock (Sync)
		    {
		        Trace.WriteLine(severity, message.FormatMessage(_typeToLog, values));
		    }
		}

		public TraceLogger(Type typeToLog, Threshold threshold = Threshold.Info)
		{
			_typeToLog = typeToLog;
			_threshold = threshold;
		}

		private static readonly object Sync = new object();
		private readonly Type _typeToLog;
		private readonly Threshold _threshold;
	}
}