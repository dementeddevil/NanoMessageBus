namespace NanoMessageBus
{
	using System;
	using Autofac;

	public partial class AutofacDependencyResolver : IDependencyResolver
	{
		public virtual T As<T>() where T : class
		{
			return this.container as T;
		}
		public virtual IDependencyResolver CreateNestedResolver()
		{
			return new AutofacDependencyResolver(this.BeginLifetimeScope());
		}
		protected virtual ILifetimeScope BeginLifetimeScope()
		{
			if (this.register == null)
				return this.container.BeginLifetimeScope();

			return this.container.BeginLifetimeScope(builder => this.register(this.depth, builder));
		}

		public AutofacDependencyResolver(IComponentContext context, Action<int, ContainerBuilder> register = null)
			: this(context.Resolve<ILifetimeScope>(), register)
		{
		}
		public AutofacDependencyResolver(ILifetimeScope container, Action<int, ContainerBuilder> register = null)
			: this(container, 0, register)
		{
		}
		private AutofacDependencyResolver(ILifetimeScope container, int depth, Action<int, ContainerBuilder> register)
		{
			if (container == null)
				throw new ArgumentNullException("container");

			this.container = container;
			this.register = register;
			this.depth = depth;
		}
		~AutofacDependencyResolver()
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

		private readonly ILifetimeScope container;
		private readonly Action<int, ContainerBuilder> register;
		private readonly int depth;
	}
}