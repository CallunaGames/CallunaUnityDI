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

		protected TItem TakeItem(Resolver resolver, PrefabInstantiationArguments instantiationArguments)
		{
			TItem item = _cache.Take<TItem>(_prefabHash);
			Transform itemTransform = item.transform;
			InitItem(itemTransform, instantiationArguments);
			_injector.InjectIntoContextHierarchy(itemTransform, resolver);
			_gameObjectInitializer.PerformActionOnHierarchy(itemTransform);
			_objectActivator.Activate(item.gameObject);
			return item;
		}

		public void Return(TItem item)
		{
			GameObject gameObject = item.gameObject;
			_gameObjectCleaner.PerformActionOnHierarchy(item.transform);
			_objectActivator.Disable(gameObject);
			_reseter.Reset(gameObject);
			_cache.Store(_prefabHash, item);
		}

		private void InitItem(Transform item, PrefabInstantiationArguments args)
		{
			if (args.WorldPositionStays.HasValue && args.WorldPositionStays.Value)
			{
				item.SetParent(args.Parent, true);
			}
			else
			{
				item.SetParent(args.Parent);
				if (args.Position.HasValue)
				{
					item.position = args.Position.Value;
				}
			}

			if (args.Rotation.HasValue)
			{
				item.rotation = args.Rotation.Value;
			}

			if (args.Scale.HasValue)
			{
				item.localScale = args.Scale.Value;
			}

			if (args.FitRectTransform.HasValue &&
			    args.FitRectTransform.Value &&
			    item.transform is RectTransform rectTransform)
			{
				rectTransform.sizeDelta = Vector2.one;
				rectTransform.anchoredPosition = Vector2.zero;
			}
		}
	}
}
