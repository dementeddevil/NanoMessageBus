#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using Machine.Specifications;

	[Subject(typeof(ExtensionMethods))]
	public class when_trying_to_retreive_a_value_from_a_null_dictionary
	{
		Because of = () =>
			thrown = Catch.Exception(() => NullDictionary.TryGetValue("key"));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();

		static readonly IDictionary<string, string> NullDictionary = null;
		static Exception thrown;
	}

	[Subject(typeof(ExtensionMethods))]
	public class when_attempting_to_retreive_a_nonexistent_value_from_a_dictionary
	{
		Because of = () =>
			value = Empty.TryGetValue("non-existent key");

		It should_return_the_default_value = () =>
			value.ShouldBeNull();

		static readonly IDictionary<string, string> Empty = new Dictionary<string, string>();
		static string value;
	}

	[Subject(typeof(ExtensionMethods))]
	public class when_attempting_to_retreive_a_value_from_a_dictionary
	{
		Establish context = () =>
			Populated.Add("key", "42");

		Because of = () =>
			value = Populated.TryGetValue("key");

		It should_return_the_expected_value = () =>
			value.ShouldEqual("42");

		static readonly IDictionary<string, string> Populated = new Dictionary<string, string>();
		static string value;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169