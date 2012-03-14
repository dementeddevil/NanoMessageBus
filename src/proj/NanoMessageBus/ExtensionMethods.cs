namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;

	public static class ExtensionMethods
	{
		public static string FormatWith(this string format, params object[] values)
		{
			return string.Format(CultureInfo.InvariantCulture, format ?? string.Empty, values);
		}

		public static string ToNull(this Guid value)
		{
			return value == Guid.Empty ? null : value.ToString();
		}

		public static TValue TryGetValue<TKey, TValue>(this IDictionary<TKey, TValue> values, TKey key)
		{
			TValue value;
			return values.TryGetValue(key, out value) ? value : default(TValue);
		}
	}
}