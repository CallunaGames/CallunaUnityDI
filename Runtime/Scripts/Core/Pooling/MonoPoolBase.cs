using UnityEngine;

namespace SBaier.DI
{
    public abstract class MonoPoolBase<TItem> : Injectable where TItem : Component
	{
		private GameObjectInjector _injector;
		private GameObjectLifeCycleActionCaller<Initializable> _gameObjectInitializer;
		private GameObjectLifeCycleActionCaller<Cleanable> _gameObjectCleaner;
		private GameObjectContextsReseter _reseter;
		private MonoPoolCache _cache;
		private TItem _prefab;
		private ObjectActivator _objectActivator;
		private int _prefabHash;

		protected bool HasStoredItem => _cache.HasObjects(_prefabHash);

		public virtual void Inject(Resolver resolver)
		{
			_injector = resolver.Resolve<GameObjectInjector>();
			_reseter = resolver.Resolve<GameObjectContextsReseter>();
			_prefab = resolver.Resolve<TItem>();
			_cache = resolver.Resolve<MonoPoolCache>();
			_objectActivator = resolver.Resolve<ObjectActivator>();
			_gameObjectInitializer = resolver.Resolve<GameObjectLifeCycleActionCaller<Initializable>>();
			_gameObjectCleaner = resolver.Resolve<GameObjectLifeCycleActionCaller<Cleanable>>();
			_prefabHash = _prefab.GetHashCode();
		}

		protected TItem TakeItem(Resolver resolver)
		{
			TItem item = _cache.Take<TItem>(_prefabHash);
			_injector.InjectIntoContextHierarchy(item.transform, resolver);
			_gameObjectInitializer.PerformLifeCycleActionOnHierarchy(item.transform);
			_objectActivator.Activate(item.gameObject);
			return item;
		}

		public void Return(TItem item)
		{
			GameObject gameObject = item.gameObject;
			_gameObjectCleaner.PerformLifeCycleActionOnHierarchy(item.transform);
			_objectActivator.Disable(gameObject);
			_reseter.Reset(gameObject);
			_cache.Store(_prefabHash, item);
		}
	}
}
