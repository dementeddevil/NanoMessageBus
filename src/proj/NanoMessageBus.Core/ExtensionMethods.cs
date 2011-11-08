namespace NanoMessageBus
{
	using System;
	using System.Globalization;

	public static class ExtensionMethods
	{
		public static string FormatWith(this string value, params object[] values)
		{
			return String.Format(CultureInfo.InvariantCulture, value, values);
		}

		public static TimeSpan Milliseconds(this int milliseconds)
		{
			return new TimeSpan(0, 0, 0, 0, milliseconds);
		}
	}
}