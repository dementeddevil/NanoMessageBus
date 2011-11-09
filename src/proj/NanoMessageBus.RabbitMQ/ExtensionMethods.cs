namespace NanoMessageBus.RabbitMQ
{
	using System;
	using System.Globalization;

	internal static class ExtensionMethods
	{
		public static string FormatWith(this string value, params object[] values)
		{
			return string.Format(CultureInfo.InvariantCulture, value, values);
		}

		public static TimeSpan Milliseconds(this int milliseconds)
		{
			return new TimeSpan(0, 0, 0, 0, milliseconds);
		}

		public static Guid ToGuid(this string value)
		{
			Guid parsed;
			return Guid.TryParse(value ?? string.Empty, out parsed) ? parsed : Guid.Empty;
		}

		public static string ToNull(this Guid value)
		{
			return Guid.Empty == value ? null : value.ToString();
		}

		public static DateTime ToDateTime(this string value)
		{
			DateTime parsed;
			return DateTime.TryParse(value, out parsed) ? parsed : DateTime.MaxValue;
		}

		public static int ToInt(this string value)
		{
			int parsed;
			return int.TryParse(value, out parsed) ? parsed : 0;
		}
	}
}