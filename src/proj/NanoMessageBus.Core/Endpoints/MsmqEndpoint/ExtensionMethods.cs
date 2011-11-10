namespace NanoMessageBus.Endpoints.MsmqEndpoint
{
	using System;
	using System.Text;

	internal static class ExtensionMethods
	{
		private const string Spacer = "\r\n----\r\n";
		private const string Separator = "\r\n-----------------------\r\n\r\n";

		public static byte[] Serialize(this Exception exception)
		{
			var builder = new StringBuilder();
			exception.Serialize(builder);
			return builder.ToString().ToByteArray();
		}
		private static void Serialize(this Exception exception, StringBuilder builder)
		{
			if (exception == null)
				return;

			if (builder.Length > 0)
				builder.Append(Separator);

			builder.Append(exception.Message);
			builder.Append(Spacer);
			builder.Append(exception.StackTrace);
		}
		private static byte[] ToByteArray(this string value)
		{
			return string.IsNullOrEmpty(value) ? new byte[0] : Encoding.UTF8.GetBytes(value);
		}
	}
}