#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.Serialization
{
	using System;
	using System.IO;
	using Machine.Specifications;

	[Subject(typeof(JsonSerializer))]
	public class when_a_new_instance_is_created : using_the_json_serializer
	{
		It should_have_an_empty_content_encoding = () =>
			serializer.ContentEncoding.ShouldEqual("utf8");

		It should_have_a_content_format_of_binary = () =>
			serializer.ContentFormat.ShouldEqual("json");
	}

	[Subject(typeof(JsonSerializer))]
	public class when_serializing_a_null_value : using_the_json_serializer
	{
		Because of = () =>
			Try(() => serializer.Serialize(stream, null));

		It should_not_populate_the_provided_stream = () =>
			stream.Length.ShouldEqual(0);
	}

	[Subject(typeof(JsonSerializer))]
	public class when_serializing_to_a_null_stream : using_the_json_serializer
	{
		Because of = () =>
			Try(() => serializer.Serialize(null, string.Empty));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(JsonSerializer))]
	public class when_deserializing_a_null_stream : using_the_json_serializer
	{
		Because of = () =>
			Try(() => serializer.Deserialize<string>(null, string.Empty, string.Empty));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(JsonSerializer))]
	public class when_serializing_a_complex_type : using_the_json_serializer
	{
		Establish context = () =>
			serializer.Serialize(stream, original);

		Because of = () =>
			deserialized = serializer.Deserialize<MyComplexType>(stream.ToArray(), string.Empty);

		It should_be_able_to_deserialize_it = () =>
		{
			deserialized.First.ShouldEqual(original.First);
			deserialized.Second.ShouldEqual(original.Second);
			deserialized.Third.ShouldEqual(original.Third);
			deserialized.Fourth.ShouldEqual(original.Fourth);
			deserialized.Fifth.ShouldEqual(original.Fifth);
			deserialized.Sixth.ShouldEqual(original.Sixth);
			deserialized.Seventh.ShouldEqual(original.Seventh);
			deserialized.Eighth.ShouldEqual(original.Eighth);
			deserialized.Ninth.ShouldEqual(original.Ninth);
			deserialized.Tenth.ShouldEqual(original.Tenth);
		};

		static MyComplexType deserialized;
		static readonly MyComplexType original = new MyComplexType
		{
			First = 1,
			Second = 2,
			Third = 3,
			Fourth = 4,
			Fifth = 5,
			Sixth = Guid.NewGuid(),
			Seventh = "7th",
			Eighth = new Uri("http://domain.com/path/query?#hash", UriKind.Absolute),
			Ninth = SystemTime.UtcNow,
			Tenth = Values.Third
		};
	}

	[Subject(typeof(JsonSerializer))]
	public abstract class using_the_json_serializer
	{
		Establish context = () =>
			stream = new MemoryStream();

		protected static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		protected static JsonSerializer serializer = new JsonSerializer();
		protected static MemoryStream stream;
		protected static Exception thrown;
	}

	[Serializable]
	internal class MyComplexType
	{
		public int First { get; set; }
		public long Second { get; set; }
		public decimal Third { get; set; }
		public ushort Fourth { get; set; }
		public byte Fifth { get; set; }
		public Guid Sixth { get; set; }
		public string Seventh { get; set; }
		public Uri Eighth { get; set; }
		public DateTime Ninth { get; set; }
		public Values Tenth { get; set; }
	}

	internal enum Values
	{
		First,
		Second,
		Third,
		Fourth
		
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169