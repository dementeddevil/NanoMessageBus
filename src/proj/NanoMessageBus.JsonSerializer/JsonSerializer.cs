﻿namespace NanoMessageBus.Serialization
{
	using System;
	using System.IO;
	using System.Runtime.Serialization.Formatters;
	using System.Text;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Converters;
	using JsonNetSerializer = Newtonsoft.Json.JsonSerializer;

	public class JsonSerializer : ISerializer
	{
		public virtual string ContentEncoding
		{
			get { return "utf8"; }
		}
		public virtual string ContentFormat
		{
			get { return "json"; }
		}

		public virtual void Serialize(Stream output, object graph)
		{
			if (graph == null)
				return;

			var encoding = this.GetEncoding(this.ContentEncoding);
			using (var streamWriter = new StreamWriter(output, encoding))
				this.Serialize(new JsonTextWriter(streamWriter), graph);
		}
		protected virtual void Serialize(JsonWriter writer, object graph)
		{
			using (writer)
				this.serializer.Serialize(writer, graph);
		}

		public virtual object Deserialize(Stream input, Type type, string format, string contentEncoding = "utf8")
		{
			using (var streamReader = new StreamReader(input, this.GetEncoding(contentEncoding)))
				return this.Deserialize(new JsonTextReader(streamReader), type);
		}
		protected virtual object Deserialize(JsonReader reader, Type type)
		{
			using (reader)
				return this.serializer.Deserialize(reader, type);
		}
		protected virtual Encoding GetEncoding(string contentEncoding)
		{
			return DefaultEncoding; // future: support alternate encodings
		}

		private const bool WriteByteOrderMarks = false;
		private static readonly Encoding DefaultEncoding = new UTF8Encoding(WriteByteOrderMarks);
		private readonly JsonNetSerializer serializer = new JsonNetSerializer
		{
#if DEBUG
			Formatting = Formatting.Indented,
#endif
			TypeNameHandling = TypeNameHandling.All,
			TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
			DefaultValueHandling = DefaultValueHandling.Ignore,
			NullValueHandling = NullValueHandling.Ignore,
			MissingMemberHandling = MissingMemberHandling.Ignore,
			DateTimeZoneHandling = DateTimeZoneHandling.Utc,
			DateFormatHandling = DateFormatHandling.IsoDateFormat,
			DateParseHandling = DateParseHandling.DateTime,
			Converters = { new StringEnumConverter() }
		};
	}
}