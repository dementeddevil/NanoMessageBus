namespace NanoMessageBus.Logging
{
	using System;

	/// <summary>
	/// Provides the ability to get a new instance of the configured logger.
	/// </summary>
	public static class LogFactory
	{
		/// <summary>
		/// Initializes static members of the LogFactory class.
		/// </summary>
		static LogFactory()
		{
			LogWith(new NullLogger());
		}

		/// <summary>
		/// Directs all logging output to the logger specified.
		/// </summary>
		/// <param name="logger">The logger to which all logging information should be directed.</param>
		public static void LogWith(ILog logger)
		{
			logger = logger ?? new NullLogger();
			LogWith(type => logger);
		}

		/// <summary>
		/// Directs all logging output to the logger callback specified.
		/// </summary>
		/// <param name="logger">The logger to which all logging information should be directed.</param>
		public static void LogWith(Func<Type, ILog> logger)
		{
			var nullLogger = new NullLogger();
			configured = logger ?? (type => nullLogger);
		}

		/// <summary>
		/// Obtains a reference to the configured logger instance.
		/// </summary>
		/// <param name="typeToLog">The type to be logged.</param>
		/// <returns>A reference to the configured logger instance</returns>
		public static ILog Build(Type typeToLog)
		{
			return configured(typeToLog);
		}

		private static Func<Type, ILog> configured;

		private class NullLogger : ILog
		{
			public void Verbose(string message, params object[] values)
			{
			}
			public void Verbose(string message, Exception exception)
			{
			}
			public void Debug(string message, params object[] values)
			{
			}
			public void Debug(string message, Exception exception)
			{
			}
			public void Info(string message, params object[] values)
			{
			}
			public void Info(string message, Exception exception)
			{
			}
			public void Warn(string message, params object[] values)
			{
			}
			public void Warn(string message, Exception exception)
			{
			}
			public void Error(string message, params object[] values)
			{
			}
			public void Error(string message, Exception exception)
			{
			}
			public void Fatal(string message, params object[] values)
			{
			}
			public void Fatal(string message, Exception exception)
			{
			}
		}
	}
}