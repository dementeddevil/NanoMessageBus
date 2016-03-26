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
			{
			    throw new ArgumentNullException(nameof(values));
			}

		    TValue value;
			return values.TryGetValue(key, out value) ? value : default(TValue);
		}

		public static void TrySetValue<TKey, TValue>(this IDictionary<TKey, TValue> values, TKey key, TValue value)
		{
			if (values != null && !values.ContainsKey(key))
			{
			    values[key] = value;
			}
		}

		internal static Guid MessageId(this ChannelEnvelope envelope)
		{
			return envelope == null || envelope.Message == null ? Guid.Empty : envelope.Message.MessageId;
		}

		public static void TryDispose(this IDisposable resource, bool rethrow = false)
		{
			if (resource == null)
			{
			    return;
			}

		    try
			{
				resource.Dispose();
			}
			catch (Exception e)
			{
				Log.Warn("Disposing resource of type '{0}' threw an exception.".FormatWith(resource.GetType()), e);
				if (rethrow)
				{
				    throw;
				}
			}
		}

		public static string ToIsoString(this DateTime value)
		{
			return value.ToString(Iso8601);
		}

		public static void Add<TContainer, TMessage>(
			this IRoutingTable table,
			Func<TContainer, IMessageHandler<TMessage>> callback,
			int sequence = int.MaxValue,
			Type handlerType = null)
			where TContainer : class
		{
			table.Add(x => callback(x.CurrentResolver.As<TContainer>()), sequence, handlerType);
		}

		private const string Iso8601 = "o";
		private static readonly ILog Log = LogFactory.Build(typeof(ExtensionMethods));
	}
}