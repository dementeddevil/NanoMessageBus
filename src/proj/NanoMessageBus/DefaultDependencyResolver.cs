namespace NanoMessageBus
{
	using System;
	using Logging;

	public class DefaultDependencyResolver<T> : IDependencyResolver
		where T : class, IDisposable
	{
		public virtual TActual As<TActual>() where TActual : class
		{
			return this.container as TActual;
		}
		public virtual IDependencyResolver CreateNestedResolver()
		{
			// TODO BUG: if a inner resolver cannot be created, we should still return a new instance
			// but wrap T in some kind of indisposable wrapper to ensure it doesn't get disposed
			// Without this fix, disposing "this" will kill the container--even a root container
			if (this.create == null)
			{
				Log.Verbose("No create callback specified, cannot create nested resolver.");
				return this;
			}

			var inner = this.create(this.container, this.depth + 1);
			if (inner == null)
			{
				Log.Verbose("Create callback did not yield a new container, cannot create nested resolver.");
				return this;
			}

			Log.Verbose("New nested resolver created at depth {0}.", this.depth + 1);
			return new DefaultDependencyResolver<T>(inner, this.create, this.depth + 1);
		}

		public DefaultDependencyResolver(T container, Func<T, int, T> create = null)
			: this(container, create, 0) { }
		private DefaultDependencyResolver(T container, Func<T, int, T> create, int depth)
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

		private static readonly ILog Log = LogFactory.Build(typeof(DefaultDependencyResolver<T>));
		private readonly T container;
		private readonly Func<T, int, T> create;
		private readonly int depth;
	}
}