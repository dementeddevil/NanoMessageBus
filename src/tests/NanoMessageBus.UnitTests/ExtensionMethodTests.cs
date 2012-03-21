#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

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

	[Subject(typeof(ExtensionMethods))]
	public class when_attempting_to_set_an_existing_key_in_a_dictionary
	{
		Establish context = () =>
			Populated.Add("key", "42");

		Because of = () =>
			Populated.TrySetValue("key", null);

		It should_not_touch_the_existing_value = () =>
			Populated["key"].ShouldEqual("42");

		static readonly IDictionary<string, string> Populated = new Dictionary<string, string>();
	}

	[Subject(typeof(ExtensionMethods))]
	public class when_attempting_to_set_a_nonexistent_key_in_a_dictionary
	{
		Because of = () =>
			Populated.TrySetValue("key", "some key");

		It should_set_the_key_to_the_new_value = () =>
			Populated["key"].ShouldEqual("some key");

		static readonly IDictionary<string, string> Populated = new Dictionary<string, string>();
	}

	[Subject(typeof(ExtensionMethods))]
	public class when_attempting_to_dispose_a_null_resource
	{
		Because of = () =>
			thrown = Catch.Exception(() => ((IDisposable)null).TryDispose());

		It should_no_do_anything = () =>
			thrown.ShouldBeNull();

		static Exception thrown;
	}

	[Subject(typeof(ExtensionMethods))]
	public class when_attempting_to_dispose_a_resource
	{
		Establish context = () =>
			mockResource = new Mock<IDisposable>();

		Because of = () =>
			mockResource.Object.TryDispose();

		It should_dispose_the_resource = () =>
			mockResource.Verify(x => x.Dispose(), Times.Once());

		static Mock<IDisposable> mockResource;
	}

	[Subject(typeof(ExtensionMethods))]
	public class when_disposing_a_resource_throws_an_exception
	{
		Establish context = () =>
		{
			mockResource = new Mock<IDisposable>();
			mockResource.Setup(x => x.Dispose()).Throws(new Exception());
		};

		Because of = () =>
			thrown = Catch.Exception(() => mockResource.Object.TryDispose());

		It should_dispose_the_resource = () =>
			mockResource.Verify(x => x.Dispose(), Times.Once());

		It should_NOT_throw_an_exception = () =>
			thrown.ShouldBeNull();

		static Mock<IDisposable> mockResource;
		static Exception thrown;
	}

	[Subject(typeof(ExtensionMethods))]
	public class when_the_caller_requests_an_exception_caught_during_dispose_should_be_rethrown
	{
		Establish context = () =>
		{
			mockResource = new Mock<IDisposable>();
			mockResource.Setup(x => x.Dispose()).Throws(toThrow);
		};

		Because of = () =>
			thrown = Catch.Exception(() => mockResource.Object.TryDispose(true));

		It should_dispose_the_resource = () =>
			mockResource.Verify(x => x.Dispose(), Times.Once());

		It should_throw_an_exception = () =>
			thrown.ShouldEqual(toThrow);

		static readonly Exception toThrow = new Exception();
		static Mock<IDisposable> mockResource;
		static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169