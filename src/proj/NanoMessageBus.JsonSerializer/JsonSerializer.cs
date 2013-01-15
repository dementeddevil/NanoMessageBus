namespace NanoMessageBus.Serialization
{
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
#if DEBUG
			writer.Formatting = Formatting.Indented;
#endif

			using (writer)
				this.serializer.Serialize(writer, graph);
		}

		public virtual T Deserialize<T>(Stream input, string format, string contentEncoding = "utf8")
		{
			using (var streamReader = new StreamReader(input, this.GetEncoding(contentEncoding)))
				return this.Deserialize<T>(new JsonTextReader(streamReader));
		}
		protected virtual T Deserialize<T>(JsonReader reader)
		{
			using (reader)
				return (T)this.serializer.Deserialize(reader, typeof(T));
		}
		protected virtual Encoding GetEncoding(string contentEncoding)
		{
			return DefaultEncoding; // future: support alternate encodings
		}

		private const bool WriteByteOrderMarks = false;
		private static readonly Encoding DefaultEncoding = new UTF8Encoding(WriteByteOrderMarks);
		private readonly JsonNetSerializer serializer = new JsonNetSerializer
		{
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