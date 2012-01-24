namespace NanoMessageBus.JsonSerializer
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;
	using Newtonsoft.Json.Bson;

	public class BsonSerializer : JsonSerializer
	{
		public override string ContentEncoding
		{
			get { return string.Empty; }
		}
		public override string ContentFormat
		{
			get { return "bson"; }
		}

		public override void Serialize(Stream output, object graph)
		{
			if (graph == null)
				return;

			var writer = new BsonWriter(output) { DateTimeKindHandling = DateTimeKind.Utc };
			this.Serialize(writer, graph);
		}
		public override T Deserialize<T>(Stream input, string format, string contentEncoding = "utf8")
		{
			var reader = new BsonReader(input, IsArray(typeof(T)), DateTimeKind.Utc);
			return this.Deserialize<T>(reader);
		}
		private static bool IsArray(Type type)
		{
			return typeof(IEnumerable).IsAssignableFrom(type) && !typeof(IDictionary<,>).IsAssignableFrom(type);
		}
	}
}