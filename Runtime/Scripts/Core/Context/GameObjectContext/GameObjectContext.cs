using UnityEngine;

namespace Calluna.DI
{
	[DisallowMultipleComponent]
	[DefaultExecutionOrder(-9998)]
    internal class GameObjectContext : MonoContext
    {
        private ChildDIContext _currentContext;
        private GameObjectInjector _injector;
        protected override DIContext DIContext => _currentContext;
        
        protected override void DoInit(Resolver resolver)
		{
			if (_currentContext == null)
			{
				_injector       = resolver.Resolve<GameObjectInjector>();
				_currentContext = CreateDIContext(resolver);
			}
			else
			{
				_currentContext.Reinitialize(resolver);
			}
		}

		private ChildDIContext CreateDIContext(Resolver resolver)
		{
			Factory<ChildDIContext, Resolver> contextFactory = resolver.Resolve<Factory<ChildDIContext, Resolver>>();
			return contextFactory.Create(resolver);
		}

		// Without "(Clone)", so the contexts of a pooled prefab are merged in the dependency graph.
		protected override string ContextName => $"GameObjectContext {name.Replace("(Clone)", string.Empty).Trim()}";

		protected override void DoInjection()
        {
            _injector.InjectIntoHierarchy(transform, DIContext.Resolver);
        }

        protected override ContextAlreadyIsInitializedException CreateContextAlreadyIsInitializedException()
        {
            return new GameObjectContextAlreadyIsInitializedException(name);
        }
    }
}

