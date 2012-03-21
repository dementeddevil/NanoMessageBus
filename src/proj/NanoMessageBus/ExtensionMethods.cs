namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using Logging;

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
			if (values == null)
				throw new ArgumentNullException("values");

			TValue value;
			return values.TryGetValue(key, out value) ? value : default(TValue);
		}

		public static void TrySetValue<TKey, TValue>(this IDictionary<TKey, TValue> values, TKey key, TValue value)
		{
			if (values != null && !values.ContainsKey(key))
				values[key] = value;
		}

		internal static Guid MessageId(this ChannelEnvelope envelope)
		{
			return envelope == null || envelope.Message == null ? Guid.Empty : envelope.Message.MessageId;
		}

		public static void TryDispose(this IDisposable resource, bool rethrow = false)
		{
			if (resource == null)
				return;

			try
			{
				resource.Dispose();
			}
			catch (Exception e)
			{
				Log.Info("Unhandled exception of type '{0}' when disposing resource of type '{1}'. Message: {2}\nStack Trace:{3}",
					e.GetType(), resource.GetType(), e.Message, e.StackTrace);

				if (rethrow)
					throw;
			}
		}

		private static readonly ILog Log = LogFactory.Build(typeof(ExtensionMethods));
	}
}