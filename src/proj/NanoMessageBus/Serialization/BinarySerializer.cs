namespace NanoMessageBus.Serialization
{
	using System.IO;

	public class BinarySerializer : ISerializer
	{
		public void Serialize(Stream destination, object graph)
		{
		}
		public T Deserialize<T>(Stream source)
		{
			return default(T);
		}
	}
}