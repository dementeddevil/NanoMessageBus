namespace NanoMessageBus.Logging
{
	using System;
	using NLog;
	using NLog.Config;

	public class NLogLogger : ILog
	{
		public virtual void Verbose(string message, params object[] values)
		{
			if (this.trace)
				this.log.Trace(message, values);
		}
		public void Verbose(string message, Exception exception)
		{
			if (this.trace)
				this.log.TraceException(message, exception);
		}

		public virtual void Debug(string message, params object[] values)
		{
			if (this.debug)
				this.log.Debug(message, values);
		}
		public void Debug(string message, Exception exception)
		{
			if (this.debug)
				this.log.DebugException(message, exception);
		}

		public virtual void Info(string message, params object[] values)
		{
			this.log.Info(message, values);
		}
		public void Info(string message, Exception exception)
		{
			this.log.InfoException(message, exception);
		}

		public virtual void Warn(string message, params object[] values)
		{
			this.log.Warn(message, values);
		}
		public void Warn(string message, Exception exception)
		{
			this.log.WarnException(message, exception);
		}

		public virtual void Error(string message, params object[] values)
		{
			this.log.Error(message, values);
		}
		public void Error(string message, Exception exception)
		{
			this.log.ErrorException(message, exception);
		}

		public virtual void Fatal(string message, params object[] values)
		{
			this.log.Fatal(message, values);
		}
		public void Fatal(string message, Exception exception)
		{
			this.log.FatalException(message, exception);
		}

		public NLogLogger(Type typeToLog)
		{
			this.log = Factory.GetLogger(typeToLog.FullName);
			this.trace = this.log.IsTraceEnabled;
			this.debug = this.log.IsDebugEnabled;
		}
		public NLogLogger(Type typeToLog, LoggingConfiguration configuration)
		{
			this.log = new NLog.LogFactory(configuration).GetLogger(typeToLog.FullName);
			this.trace = this.log.IsTraceEnabled;
			this.debug = this.log.IsDebugEnabled;
		}

		private static readonly NLog.LogFactory Factory = new NLog.LogFactory();
		private readonly Logger log;
		private readonly bool debug;
		private readonly bool trace;
	}
}