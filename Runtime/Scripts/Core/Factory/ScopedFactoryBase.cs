namespace Calluna.DI
{
    public abstract class ScopedFactoryBase<T> : Injectable where T : new()
    {
        private Resolver _parentResolver;

        public virtual void Inject(Resolver resolver)
        {
            _parentResolver = resolver;
        }

        private static ChildDIContext CreateDIContext(Resolver resolver)
        {
            Factory<ChildDIContext, Resolver> contextFactory = resolver.Resolve<Factory<ChildDIContext, Resolver>>();
            return contextFactory.Create(resolver);
        }

        protected T DoCreation()
        {
            return DoCreation(CreateDIContext(_parentResolver));
        }

        protected T DoCreation<TArgument>(TArgument argument)
        {
            ChildDIContext childContext = CreateDIContext(_parentResolver);
            childContext.Binder.BindInstance(argument);
            return DoCreation(childContext);
        }

        private T DoCreation(ChildDIContext childContext)
        {
            try
            {
                InitScope(childContext.Resolver, childContext.Binder);
                childContext.ValidateBindings();
                T result = new T();
                (result as Injectable)?.Inject(childContext.Resolver);
                childContext.MoveInstancesToParent();
                return result;
            }
            finally
            {
                ((DIContext)childContext).Clear();
            }
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