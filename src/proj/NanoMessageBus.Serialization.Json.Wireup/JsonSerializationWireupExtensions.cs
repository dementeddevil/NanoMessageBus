namespace NanoMessageBus
{
	using Newtonsoft.Json;
	using Serialization;
	using Wireup;

	public static class JsonSerializationWireupExtensions
	{
		public static SerializationWireup WithJsonSerializer(this SerializationWireup wireup)
		{
			return wireup.CustomSerializer(new JsonMessageSerializer());
		}

		public static SerializationWireup WithJsonSerializer(this SerializationWireup wireup, JsonSerializer customSerializer)
		{
			return wireup.CustomSerializer(new JsonMessageSerializer(customSerializer));
		}

		public static SerializationWireup WithBsonSerializer(this SerializationWireup wireup)
		{
			return wireup.CustomSerializer(new BsonMessageSerializer());
		}

		public static SerializationWireup WithBsonSerializer(this SerializationWireup wireup, JsonSerializer customSerializer)
		{
			return wireup.CustomSerializer(new BsonMessageSerializer(customSerializer));
		}
	}
}