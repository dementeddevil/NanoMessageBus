namespace NanoMessageBus.Serialization
{
	using System.IO;
	using System.IO.Compression;

	public class GzipSerializer : SerializerBase
	{
		protected override void SerializePayload(Stream output, object message)
		{
			using (var compressedStream = new DeflateStream(output, CompressionMode.Compress, true))
				this.inner.Serialize(compressedStream, message);
		}
		protected override object DeserializePayload(Stream input)
		{
			using (var inflatedStream = new DeflateStream(input, CompressionMode.Decompress, true))
				return this.inner.Deserialize(inflatedStream);
		}

		public GzipSerializer(ISerializeMessages inner)
		{
			this.inner = inner;
		}

		private readonly ISerializeMessages inner;
	}
}