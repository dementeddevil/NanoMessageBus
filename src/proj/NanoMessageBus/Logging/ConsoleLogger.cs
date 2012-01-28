namespace NanoMessageBus.Logging
{
	using System;

	public class ConsoleLogger : ILog
	{
		public virtual void Verbose(string message, params object[] values)
		{
			this.Log(ConsoleColor.DarkGreen, Threshold.Verbose, message, values);
		}
		public virtual void Debug(string message, params object[] values)
		{
			this.Log(ConsoleColor.Green, Threshold.Debug, message, values);
		}
		public virtual void Info(string message, params object[] values)
		{
			this.Log(ConsoleColor.White, Threshold.Info, message, values);
		}
		public virtual void Warn(string message, params object[] values)
		{
			this.Log(ConsoleColor.Yellow, Threshold.Warn, message, values);
		}
		public virtual void Error(string message, params object[] values)
		{
			this.Log(ConsoleColor.DarkRed, Threshold.Error, message, values);
		}
		public virtual void Fatal(string message, params object[] values)
		{
			this.Log(ConsoleColor.Red, Threshold.Fatal, message, values);
		}
		protected virtual void Log(ConsoleColor color, Threshold category, string message, params object[] values)
		{
			if (category < this.threshold)
				return;

			lock (Sync)
			{
				Console.ForegroundColor = color;
				Console.WriteLine(message.FormatMessage(this.typeToLog, values));
				Console.ForegroundColor = this.originalColor;
			}
		}

		public ConsoleLogger(Type typeToLog, Threshold threshold = Threshold.Info)
		{
			this.typeToLog = typeToLog;
			this.threshold = threshold;
		}

		private static readonly object Sync = new object();
		private readonly ConsoleColor originalColor = Console.ForegroundColor;
		private readonly Type typeToLog;
		private readonly Threshold threshold;
	}
}