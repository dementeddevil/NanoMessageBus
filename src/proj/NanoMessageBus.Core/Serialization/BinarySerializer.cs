namespace NanoMessageBus.Serialization
{
	using System.IO;
	using System.Runtime.Serialization;
	using System.Runtime.Serialization.Formatters.Binary;

	public class BinarySerializer : SerializerBase
	{
		private readonly IFormatter formatter = new BinaryFormatter();

		protected override void SerializePayload(Stream output, object message)
		{
			this.formatter.Serialize(output, message);
		}
		protected override object DeserializePayload(Stream input)
		{
			return this.formatter.Deserialize(input);
		}
	}
}