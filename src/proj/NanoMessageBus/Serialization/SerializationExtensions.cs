namespace NanoMessageBus.Serialization
{
	using System.IO;

	public static class SerializationExtensions
	{
		public static byte[] Serialize(this ISerializer serializer, object graph)
		{
			using (var destination = new MemoryStream())
			{
				serializer.Serialize(destination, graph);
				return destination.ToArray();
			}
		}
		public static T Deserialize<T>(
			this ISerializer serializer, byte[] source, string format, string encoding = "")
		{
			using (var stream = new MemoryStream(source))
				return serializer.Deserialize<T>(stream, format, encoding);
		}
	}
}