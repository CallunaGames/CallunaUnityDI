namespace Calluna.DI
{
    public abstract class ScopedFactoryBase<T> : Injectable where T : new()
    {
        private ChildDIContext _childContext;
        
        public virtual void Inject(Resolver resolver)
        {
            _childContext = CreateDIContext(resolver);
        }

        private ChildDIContext CreateDIContext(Resolver resolver)
        {
            Factory<ChildDIContext, Resolver> contextFactory = resolver.Resolve<Factory<ChildDIContext, Resolver>>();
            return contextFactory.Create(resolver);
        }

        protected T DoCreation()
        {
            InitScope(_childContext.Resolver, _childContext.Binder);
            _childContext.ValidateBindings();
            T result = new T();
            (result as Injectable)?.Inject(_childContext.Resolver);
            _childContext.MoveInstancesToParent();
            ((DIContext)_childContext).Clear();
            return result;
        }

        protected T DoCreation<TArgument>(TArgument argument)
        {
            _childContext.Binder.BindInstance(argument);
            return DoCreation();
        }
        
        protected abstract void InitScope(Resolver resolver, Binder binder);
    }

    public abstract class ScopedFactory<T> : ScopedFactoryBase<T>, Factory<T> where T : new()
    {
        public T Create()
        {
            return DoCreation();
        }
    }

    public abstract class ScopedFactory<T, TArgument> : ScopedFactoryBase<T>, Factory<T, TArgument> where T : new()
    {
        public T Create(TArgument arg)
        {
            return DoCreation(arg);
        }
    }
}