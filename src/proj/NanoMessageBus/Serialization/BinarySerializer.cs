namespace NanoMessageBus.Serialization
{
	using System.IO;
	using System.Runtime.Serialization;
	using System.Runtime.Serialization.Formatters.Binary;

	public class BinarySerializer : ISerializer
	{
		public virtual string ContentEncoding
		{
			get { return string.Empty; }
		}
		public virtual string ContentFormat
		{
			get { return "binary"; }
		}

		public virtual void Serialize(Stream destination, object graph)
		{
			if (graph == null)
				return;

			this.formatter.Serialize(destination, graph);
		}
		public virtual T Deserialize<T>(Stream source, string format, string contentEncoding = "")
		{
			return (T)this.formatter.Deserialize(source);
		}

		private readonly IFormatter formatter = new BinaryFormatter();
	}
}