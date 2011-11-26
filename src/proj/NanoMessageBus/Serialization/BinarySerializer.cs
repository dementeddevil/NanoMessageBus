namespace NanoMessageBus.Serialization
{
	using System.IO;

	public class BinarySerializer : ISerializer
	{
		public virtual string ContentEncoding
		{
			get { return null; }
		}
		public virtual string ContentFormat
		{
			get { return "binary"; }
		}

		public virtual void Serialize(Stream destination, object graph)
		{
		}
		public virtual T Deserialize<T>(Stream source, string format, string contentEncoding = "")
		{
			return default(T);
		}
	}
}