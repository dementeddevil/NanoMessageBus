namespace NanoMessageBus
{
	using System;
	using System.Globalization;

	public static class ExtensionMethods
	{
		public static string FormatWith(this string format, params object[] values)
		{
			return string.Format(CultureInfo.InvariantCulture, format ?? string.Empty, values);
		}
		public static void TryDispose(this IDisposable resource)
		{
			if (resource != null)
				resource.Dispose();
		}
	}
}