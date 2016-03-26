﻿using FluentAssertions;

#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.Serialization
{
	using System;
	using System.IO;
	using Machine.Specifications;

	[Subject(typeof(BinarySerializer))]
	public class when_a_new_instance_is_created : using_the_binary_serializer
	{
		It should_have_an_empty_content_encoding = () =>
			serializer.ContentEncoding.Should().BeEmpty();

		It should_have_a_content_format_of_binary = () =>
			serializer.ContentFormat.Should().Be("binary");
	}

	[Subject(typeof(BinarySerializer))]
	public class when_serializing_a_null_value : using_the_binary_serializer
	{
		Because of = () =>
			Try(() => serializer.Serialize(stream, null));

		It should_not_populate_the_provided_stream = () =>
			stream.Length.Should().Be(0);
	}

	[Subject(typeof(BinarySerializer))]
	public class when_serializing_to_a_null_stream : using_the_binary_serializer
	{
		Because of = () =>
			Try(() => serializer.Serialize(null, string.Empty));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ArgumentNullException>();
	}

	[Subject(typeof(BinarySerializer))]
	public class when_deserializing_a_null_stream : using_the_binary_serializer
	{
		Because of = () =>
			Try(() => serializer.Deserialize(null, typeof(string), string.Empty, string.Empty));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ArgumentNullException>();
	}

	[Subject(typeof(BinarySerializer))]
	public class when_serializing_a_complex_type : using_the_binary_serializer
	{
		Establish context = () =>
			serializer.Serialize(stream, original);

		Because of = () =>
			deserialized = (MyComplexType)serializer.Deserialize(stream.ToArray(), typeof(MyComplexType), string.Empty);

		It should_be_able_to_deserialize_it = () =>
		{
			deserialized.First.Should().Be(original.First);
			deserialized.Second.Should().Be(original.Second);
			deserialized.Third.Should().Be(original.Third);
			deserialized.Fourth.Should().Be(original.Fourth);
			deserialized.Fifth.Should().Be(original.Fifth);
			deserialized.Sixth.Should().Be(original.Sixth);
			deserialized.Seventh.Should().Be(original.Seventh);
			deserialized.Eighth.Should().Be(original.Eighth);
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
			Eighth = new Uri("http://localhost/eighth")
		};
	}

	[Subject(typeof(BinarySerializer))]
	public abstract class using_the_binary_serializer
	{
		Establish context = () =>
			stream = new MemoryStream();

		protected static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		protected static BinarySerializer serializer = new BinarySerializer();
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
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169, 414