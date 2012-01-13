#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
	using System;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject(typeof(DefaultDependencyResolver<IDisposable>))]
	public class when_creating_a_root_dependency_resolver_with_a_null_container : using_the_dependency_resolver
	{
		Because of = () =>
			Try(() => Build(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultDependencyResolver<IDisposable>))]
	public class when_creating_a_root_dependency_resolver_with_a_container : using_the_dependency_resolver
	{
		Because of = () =>
			Build(mockRootContainer.Object);

		It should_expose_the_actual_container_provided = () =>
			rootResolver.Actual<IDisposable>().ShouldEqual(mockRootContainer.Object);
	}

	[Subject(typeof(DefaultDependencyResolver<IDisposable>))]
	public class when_no_nested_container_callback_is_provided : using_the_dependency_resolver
	{
		Establish context = () =>
			Build(mockRootContainer.Object, null);

		Because of = () =>
			nestedResolver = rootResolver.CreateNestedResolver();

		It should_return_a_self_reference_when_attempting_to_create_a_nested_resolver = () =>
			rootResolver.ShouldEqual(nestedResolver);
	}

	[Subject(typeof(DefaultDependencyResolver<IDisposable>))]
	public class when_the_nested_container_callback_does_not_return_a_value : using_the_dependency_resolver
	{
		Establish context = () =>
			Build(mockRootContainer.Object, (parent, name) => null);

		Because of = () =>
			nestedResolver = rootResolver.CreateNestedResolver();

		It should_return_a_self_reference_when_attempting_to_create_a_nested_resolver = () =>
			rootResolver.ShouldEqual(nestedResolver);
	}

	[Subject(typeof(DefaultDependencyResolver<IDisposable>))]
	public class when_the_nested_container_callback_is_invoked : using_the_dependency_resolver
	{
		Establish context = () => Build(mockRootContainer.Object, (parent, name) =>
		{
			parentContainerProvided = parent;
			nameProvided = name;
			return mockNestedContainer.Object;
		});

		Because of = () =>
			nestedResolver = rootResolver.CreateNestedResolver("Hello, World!");

		It should_provide_the_parent_container_to_the_callback_as_a_parameter = () =>
			parentContainerProvided.ShouldEqual(mockRootContainer.Object);

		It should_provide_the_name_provided_to_the_callback_as_a_parameter = () =>
			nameProvided.ShouldEqual("Hello, World!");

		It should_return_a_reference_to_the_nested_container = () =>
			nestedResolver.Actual<IDisposable>().ShouldEqual(mockNestedContainer.Object);

		static IDisposable parentContainerProvided;
		static string nameProvided;
	}

	[Subject(typeof(DefaultDependencyResolver<IDisposable>))]
	public class when_disposing_the_root_resolver : using_the_dependency_resolver
	{
		Because of = () =>
			rootResolver.Dispose();

		It should_NOT_dispose_the_underlying_container = () =>
			mockRootContainer.Verify(x => x.Dispose(), Times.Never());

		It should_NOT_dispose_the_any_nested_container = () =>
			mockRootContainer.Verify(x => x.Dispose(), Times.Never());
	}

	[Subject(typeof(DefaultDependencyResolver<IDisposable>))]
	public class when_disposing_a_nested_resolver : using_the_dependency_resolver
	{
		Establish context = () =>
			nestedResolver = rootResolver.CreateNestedResolver();

		Because of = () =>
			nestedResolver.Dispose();

		It should_NOT_dispose_the_root_container = () =>
			mockRootContainer.Verify(x => x.Dispose(), Times.Never());

		It should_dispose_the_nested_container = () =>
			mockNestedContainer.Verify(x => x.Dispose(), Times.Once());
	}

	public abstract class using_the_dependency_resolver
	{
		Establish context = () =>
		{
			mockRootContainer = new Mock<IDisposable>();
			mockNestedContainer = new Mock<IDisposable>();
			nestedResolver = null;
			thrown = null;

			Build(mockRootContainer.Object);
		};
		protected static void Build(IDisposable container)
		{
			Build(container, createdNested);
		}
		protected static void Build(IDisposable container, Func<IDisposable, string, IDisposable> create)
		{
			rootResolver = new DefaultDependencyResolver<IDisposable>(container, create);
		}
		protected static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		protected static DefaultDependencyResolver<IDisposable> rootResolver;
		protected static Mock<IDisposable> mockRootContainer;
		protected static Mock<IDisposable> mockNestedContainer;
		protected static Func<IDisposable, string, IDisposable> createdNested = (parent, name) => mockNestedContainer.Object;
		protected static IDependencyResolver nestedResolver;
		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169