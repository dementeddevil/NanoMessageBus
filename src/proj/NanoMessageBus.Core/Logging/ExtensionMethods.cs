namespace NanoMessageBus.Logging
{
	using System;
	using System.Globalization;
	using System.Threading;

	internal static class ExtensionMethods
	{
		private const string MessageFormat = "{0:yyyyMMdd.HHmmss.ff} - {1} - {2} - {3}";

		public static string FormatMessage(this string message, Type typeToLog, params object[] values)
		{
			return MessageFormat.FormatWith(
				DateTime.UtcNow,
				Thread.CurrentThread.GetName(),
				typeToLog.FullName,
				message.FormatWith(values));
		}
		private static string GetName(this Thread thread)
		{
			if (thread == null)
				return string.Empty;

			return string.IsNullOrEmpty(thread.Name)
				? thread.ManagedThreadId.ToString(CultureInfo.InvariantCulture) : thread.Name;
		}
	}
}