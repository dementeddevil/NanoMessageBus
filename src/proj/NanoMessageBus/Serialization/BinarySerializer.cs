namespace NanoMessageBus.Serialization
{
	using System;
	using System.IO;
	using System.Runtime.Serialization;
	using System.Runtime.Serialization.Formatters.Binary;

	public class BinarySerializer : ISerializer
	{
		public virtual string ContentEncoding => string.Empty;
	    public virtual string ContentFormat => "binary";

	    public virtual void Serialize(Stream destination, object graph)
		{
			if (graph == null)
			{
			    return;
			}

	        _formatter.Serialize(destination, graph);
		}
		public virtual object Deserialize(Stream source, Type type, string format, string contentEncoding = "")
		{
			return _formatter.Deserialize(source);
		}

		private readonly IFormatter _formatter = new BinaryFormatter();
	}
}