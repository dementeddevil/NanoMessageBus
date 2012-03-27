namespace NanoMessageBus.Logging
{
	using System;

	public class Log4NetLogger : ILog
	{
		public virtual void Verbose(string message, params object[] values)
		{
			// no op
		}
		public virtual void Debug(string message, params object[] values)
		{
			if (this.debug)
				this.log.DebugFormat(message, values);
		}
		public virtual void Info(string message, params object[] values)
		{
			this.log.InfoFormat(message, values);
		}
		public virtual void Warn(string message, params object[] values)
		{
			this.log.WarnFormat(message, values);
		}
		public virtual void Error(string message, params object[] values)
		{
			this.log.ErrorFormat(message, values);
		}
		public virtual void Fatal(string message, params object[] values)
		{
			this.log.FatalFormat(message, values);
		}

		public Log4NetLogger(Type typeToLog, bool autoConfigure = true)
		{
			if (autoConfigure)
				log4net.Config.XmlConfigurator.Configure();

			this.log = log4net.LogManager.GetLogger(typeToLog);
			this.debug = this.log.IsDebugEnabled;
		}

		private readonly log4net.ILog log;
		private readonly bool debug;
	}
}