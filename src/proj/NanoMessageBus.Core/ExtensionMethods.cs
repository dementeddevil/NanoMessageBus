namespace NanoMessageBus
{
	using System.Globalization;

	public static class ExtensionMethods
	{
		public static string FormatWith(this string value, params object[] values)
		{
			return string.Format(CultureInfo.InvariantCulture, value, values);
		}
	}
}