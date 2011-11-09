namespace NanoMessageBus.Serialization
{
	using System.IO;
	using System.Runtime.Serialization;

	/// <summary>
	/// Indicates the ability to serialize and deserialize an object.
	/// </summary>
	/// <remarks>
	/// Object instances which implement this interface must be designed to be multi-thread safe.
	/// </remarks>
	public interface ISerializer
	{
		/// <summary>
		/// Gets the content MIME type associated with the serializer.
		/// </summary>
		string ContentType { get; }

		/// <summary>
		/// Gets the underling content encoding associated with the serializer.
		/// </summary>
		string ContentEncoding { get; }

		/// <summary>
		/// Serializes the object provided into the stream specified.
		/// </summary>
		/// <param name="output">The output stream into which all serialized bytes should be written.</param>
		/// <param name="payload">The object to be serialized.</param>
		/// <exception cref="SerializationException" />
		void Serialize(Stream output, object payload);

		/// <summary>
		/// Deserializes the stream specified into an object graph.
		/// </summary>
		/// <param name="input">The stream from which all serialized bytes are to be read.</param>
		/// <returns>If successful, returns a fully reconstituted object.</returns>
		/// <exception cref="SerializationException" />
		object Deserialize(Stream input);
	}
}