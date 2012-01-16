namespace NanoMessageBus
{
	using System;

	public class DefaultDependencyResolver<T> : IDependencyResolver
		where T : class, IDisposable
	{
		public virtual TActual As<TActual>() where TActual : class
		{
			return this.container as TActual;
		}
		public virtual IDependencyResolver CreateNestedResolver(string name = null)
		{
			if (this.create == null)
				return this;

			var inner = this.create(this.container, name);
			if (inner == null)
				return this;

			return new DefaultDependencyResolver<T>(inner, this.create, this.depth + 1);
		}

		public DefaultDependencyResolver(T container, Func<T, string, T> create = null)
			: this(container, create, 0) { }
		private DefaultDependencyResolver(T container, Func<T, string, T> create, int depth)
		{
			if (container == null)
				throw new ArgumentNullException("container");

			this.container = container;
			this.depth = depth;
			this.create = create;
		}
		~DefaultDependencyResolver()
		{
			this.Dispose(false);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
				this.container.Dispose();
		}

		private readonly T container;
		private readonly Func<T, string, T> create;
		private readonly int depth;
	}
}