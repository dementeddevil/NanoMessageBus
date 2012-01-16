namespace NanoMessageBus
{
	using System;

	/// <summary>
	/// Provides the ability to resolve dependencies from within user code.
	/// </summary>
	public interface IDependencyResolver : IDisposable
	{
		/// <summary>
		/// Gets a reference to the actual IoC container used to resolve dependencies.
		/// </summary>
		/// <typeparam name="T">The type of IoC container.</typeparam>
		/// <returns>A reference to the actual IoC container used to resolve dependencies.</returns>
		T As<T>() where T : class;

		/// <summary>
		/// Instructs the container to create a nested or child instance.
		/// </summary>
		/// <returns>A reference to a child instance of IDependencyResolver.</returns>
		IDependencyResolver CreateNestedResolver();
	}
}