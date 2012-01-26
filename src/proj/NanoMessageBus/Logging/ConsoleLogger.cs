namespace NanoMessageBus.Logging
{
	using System;

	public class ConsoleLogger : ILog
	{
		public virtual void Verbose(string message, params object[] values)
		{
			this.Log(ConsoleColor.DarkGreen, message, values);
		}
		public virtual void Debug(string message, params object[] values)
		{
			this.Log(ConsoleColor.Green, message, values);
		}
		public virtual void Info(string message, params object[] values)
		{
			this.Log(ConsoleColor.White, message, values);
		}
		public virtual void Warn(string message, params object[] values)
		{
			this.Log(ConsoleColor.Yellow, message, values);
		}
		public virtual void Error(string message, params object[] values)
		{
			this.Log(ConsoleColor.DarkRed, message, values);
		}
		public virtual void Fatal(string message, params object[] values)
		{
			this.Log(ConsoleColor.Red, message, values);
		}
		protected virtual void Log(ConsoleColor color, string message, params object[] values)
		{
			lock (Sync)
			{
				Console.ForegroundColor = color;
				Console.WriteLine(message.FormatMessage(this.typeToLog, values));
				Console.ForegroundColor = this.originalColor;
			}
		}

		public ConsoleLogger(Type typeToLog)
		{
			this.typeToLog = typeToLog;
		}

		private static readonly object Sync = new object();
		private readonly ConsoleColor originalColor = Console.ForegroundColor;
		private readonly Type typeToLog;
	}
}