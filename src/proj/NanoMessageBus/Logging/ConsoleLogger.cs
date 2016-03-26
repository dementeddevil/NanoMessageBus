namespace NanoMessageBus.Logging
{
	using System;

	public class ConsoleLogger : ILog
	{
		public virtual void Verbose(string message, params object[] values)
		{
			Log(ConsoleColor.DarkGreen, Threshold.Verbose, message, values);
		}
		public void Verbose(string message, Exception exception)
		{
			Log(ConsoleColor.DarkGreen, Threshold.Verbose, message, exception);
		}

		public virtual void Debug(string message, params object[] values)
		{
			Log(ConsoleColor.Green, Threshold.Debug, message, values);
		}
		public void Debug(string message, Exception exception)
		{
			Log(ConsoleColor.Green, Threshold.Debug, message, exception);
		}

		public virtual void Info(string message, params object[] values)
		{
			Log(ConsoleColor.White, Threshold.Info, message, values);
		}
		public void Info(string message, Exception exception)
		{
			Log(ConsoleColor.White, Threshold.Info, message, exception);
		}

		public virtual void Warn(string message, params object[] values)
		{
			Log(ConsoleColor.Yellow, Threshold.Warn, message, values);
		}
		public void Warn(string message, Exception exception)
		{
			Log(ConsoleColor.Yellow, Threshold.Warn, message, exception);
		}

		public virtual void Error(string message, params object[] values)
		{
			Log(ConsoleColor.DarkRed, Threshold.Error, message, values);
		}
		public void Error(string message, Exception exception)
		{
			Log(ConsoleColor.DarkRed, Threshold.Error, message, exception);
		}

		public virtual void Fatal(string message, params object[] values)
		{
			Log(ConsoleColor.Red, Threshold.Fatal, message, values);
		}
		public void Fatal(string message, Exception exception)
		{
			Log(ConsoleColor.Red, Threshold.Fatal, message, exception);
		}

		protected virtual void Log(ConsoleColor color, Threshold severity, string message, params object[] values)
		{
			if (severity < _threshold)
			{
			    return;
			}

		    lock (Sync)
			{
				Console.ForegroundColor = color;

				Console.WriteLine(message.FormatMessage(_typeToLog, values));
				Console.ForegroundColor = _originalColor;
			}
		}

		public ConsoleLogger(Type typeToLog, Threshold threshold = Threshold.Info)
		{
			_typeToLog = typeToLog;
			_threshold = threshold;
		}

		private static readonly object Sync = new object();
		private readonly ConsoleColor _originalColor = Console.ForegroundColor;
		private readonly Type _typeToLog;
		private readonly Threshold _threshold;
	}
}