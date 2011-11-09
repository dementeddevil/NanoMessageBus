namespace NanoMessageBus.Serialization
{
	using System.IO;
	using System.Runtime.Serialization;
	using System.Runtime.Serialization.Formatters.Binary;

	public class BinarySerializer : SerializerBase
	{
		public override string ContentType
		{
			get { return "application/octet-stream"; }
		}
		public override string ContentEncoding
		{
			get { return "binary"; }
		}

		protected override void SerializePayload(Stream output, object message)
		{
			this.formatter.Serialize(output, message);
		}
		protected override object DeserializePayload(Stream input)
		{
			return this.formatter.Deserialize(input);
		}

		private readonly IFormatter formatter = new BinaryFormatter();
	}
}