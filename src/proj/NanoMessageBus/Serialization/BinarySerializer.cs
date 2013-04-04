namespace NanoMessageBus.Serialization
{
	using System;
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
		public virtual object Deserialize(Stream source, Type type, string format, string contentEncoding = "")
		{
			return this.formatter.Deserialize(source);
		}

		private readonly IFormatter formatter = new BinaryFormatter();
	}
}