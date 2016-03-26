namespace NanoMessageBus.Logging
{
	using System;

	public class ConsoleLogger : ILog
	{
		public virtual void Verbose(string message, params object[] values)
		{
			this.Log(ConsoleColor.DarkGreen, Threshold.Verbose, message, values);
		}
		public void Verbose(string message, Exception exception)
		{
			this.Log(ConsoleColor.DarkGreen, Threshold.Verbose, message, exception);
		}

		public virtual void Debug(string message, params object[] values)
		{
			this.Log(ConsoleColor.Green, Threshold.Debug, message, values);
		}
		public void Debug(string message, Exception exception)
		{
			this.Log(ConsoleColor.Green, Threshold.Debug, message, exception);
		}

		public virtual void Info(string message, params object[] values)
		{
			this.Log(ConsoleColor.White, Threshold.Info, message, values);
		}
		public void Info(string message, Exception exception)
		{
			this.Log(ConsoleColor.White, Threshold.Info, message, exception);
		}

		public virtual void Warn(string message, params object[] values)
		{
			this.Log(ConsoleColor.Yellow, Threshold.Warn, message, values);
		}
		public void Warn(string message, Exception exception)
		{
			this.Log(ConsoleColor.Yellow, Threshold.Warn, message, exception);
		}

		public virtual void Error(string message, params object[] values)
		{
			this.Log(ConsoleColor.DarkRed, Threshold.Error, message, values);
		}
		public void Error(string message, Exception exception)
		{
			this.Log(ConsoleColor.DarkRed, Threshold.Error, message, exception);
		}

		public virtual void Fatal(string message, params object[] values)
		{
			this.Log(ConsoleColor.Red, Threshold.Fatal, message, values);
		}
		public void Fatal(string message, Exception exception)
		{
			this.Log(ConsoleColor.Red, Threshold.Fatal, message, exception);
		}

		protected virtual void Log(ConsoleColor color, Threshold severity, string message, params object[] values)
		{
			if (severity < this._threshold)
				return;

			lock (Sync)
			{
				Console.ForegroundColor = color;

				Console.WriteLine(message.FormatMessage(this._typeToLog, values));
				Console.ForegroundColor = this._originalColor;
			}
		}

		public ConsoleLogger(Type typeToLog, Threshold threshold = Threshold.Info)
		{
			this._typeToLog = typeToLog;
			this._threshold = threshold;
		}

		private static readonly object Sync = new object();
		private readonly ConsoleColor _originalColor = Console.ForegroundColor;
		private readonly Type _typeToLog;
		private readonly Threshold _threshold;
	}
}