namespace NanoMessageBus.Serialization
{
	using System;
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
		public static object Deserialize(this ISerializer serializer, byte[] source, Type type, string format, string encoding = "")
		{
			using (var stream = new MemoryStream(source))
				return serializer.Deserialize(stream, type, format, encoding);
		}
	}
}