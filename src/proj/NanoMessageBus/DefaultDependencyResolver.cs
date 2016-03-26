namespace NanoMessageBus
{
	using System;
	using Logging;

	public class DefaultDependencyResolver<T> : IDependencyResolver
		where T : class, IDisposable
	{
		public virtual TActual As<TActual>() where TActual : class
		{
			return this._container as TActual;
		}
		public virtual IDependencyResolver CreateNestedResolver()
		{
			if (this._create == null)
			{
				Log.Verbose("No create callback specified, cannot create nested resolver.");
				return new DefaultDependencyResolver<T>(this._container, this._create, this._depth + 1, false);
			}

			var inner = this._create(this._container, this._depth + 1);
			if (inner == null)
			{
				Log.Verbose("Create callback did not yield a new container, cannot create nested resolver.");
				return new DefaultDependencyResolver<T>(this._container, this._create, this._depth + 1, false);
			}

			Log.Verbose("New nested resolver created at depth {0}.", this._depth + 1);
			return new DefaultDependencyResolver<T>(inner, this._create, this._depth + 1, true);
		}

		public DefaultDependencyResolver(T container, Func<T, int, T> create = null)
			: this(container, create, 0, true) { }
		private DefaultDependencyResolver(T container, Func<T, int, T> create, int depth, bool disposable)
		{
			if (container == null)
				throw new ArgumentNullException(nameof(container));

			this._container = container;
			this._depth = depth;
			this._create = create;
			this._disposable = disposable;
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
			if (disposing && this._disposable)
				this._container.TryDispose();
		}

		private static readonly ILog Log = LogFactory.Build(typeof(DefaultDependencyResolver<>));
		private readonly T _container;
		private readonly Func<T, int, T> _create;
		private readonly int _depth;
		private readonly bool _disposable;
	}
}