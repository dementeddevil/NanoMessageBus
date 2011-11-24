namespace NanoMessageBus.Serialization
{
	using System.IO;

	public class BinarySerializer : ISerializer
	{
		public virtual void Serialize(Stream destination, object graph)
		{
		}
		public virtual T Deserialize<T>(Stream source, string contentEncoding = "")
		{
			return default(T);
		}
	}
}