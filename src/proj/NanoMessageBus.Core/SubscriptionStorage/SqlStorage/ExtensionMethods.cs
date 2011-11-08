namespace NanoMessageBus.SubscriptionStorage.SqlStorage
{
	using System;
	using System.Collections.Generic;
	using System.Data;

	internal static class ExtensionMethods
	{
		public static object ToNull(this DateTime? value)
		{
			return !value.HasValue || value == DateTime.MinValue || value == DateTime.MaxValue ? DBNull.Value : (object)value;
		}

		public static void AddParameter(this IDbCommand command, string parameterName, object parameterValue)
		{
			var parameter = command.CreateParameter();
			parameter.ParameterName = parameterName;
			parameter.Value = parameterValue;
			command.Parameters.Add(parameter);
		}

		public static int ExecuteWrappedCommand(this IDbCommand command)
		{
			try
			{
				return command.ExecuteNonQuery();
			}
			catch (Exception e)
			{
				throw new SubscriptionStorageException(e.Message, e);
			}
		}
		public static IEnumerable<IDataRecord> ExecuteWrappedQuery(this IDbCommand query)
		{
			IDataReader reader;
			try
			{
				reader = query.ExecuteReader();
			}
			catch (Exception e)
			{
				throw new SubscriptionStorageException(e.Message, e);
			}

			using (reader)
				while (reader.Read())
					yield return reader;
		}
	}
}