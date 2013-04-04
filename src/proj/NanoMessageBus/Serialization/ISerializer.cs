namespace NanoMessageBus.Serialization
{
	using System;
	using System.IO;

	/// <summary>
	/// Provides the ability to serialize and deserialize an object graph.
	/// </summary>
	/// <remarks>
	/// Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
	/// </remarks>
	public interface ISerializer
	{
		/// <summary>
		/// Gets the value which indicates the encoding mechanism used (gzip, bzip2, lzma, aes, etc.)
		/// </summary>
		string ContentEncoding { get; }

		/// <summary>
		/// Gets the MIME-type suffix (json, xml, binary, etc.)
		/// </summary>
		string ContentFormat { get; }

		/// <summary>
		/// Serializes the object graph provided and writes a serialized representation to the output stream provided.
		/// </summary>
		/// <param name="destination">The stream into which the serialized object graph should be written.</param>
		/// <param name="graph">The object graph to be serialized.</param>
		void Serialize(Stream destination, object graph);

		/// <summary>
		/// Deserializes the stream provided and reconstructs the corresponding object graph.
		/// </summary>
		/// <param name="source">The stream of bytes from which the object will be reconstructed.</param>
		/// <param name="type">The type to be deserialized.</param>
		/// <param name="format">The optional value which indicates the format used during serialization.</param>
		/// <param name="contentEncoding">The optional value which indicates the encoding used during serialization.</param>
		/// <returns>The reconstructed object.</returns>
		object Deserialize(Stream source, Type type, string format, string contentEncoding = "");
	}
}