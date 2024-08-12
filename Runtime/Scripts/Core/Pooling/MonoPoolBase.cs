using UnityEngine;

namespace SBaier.DI
{
    public abstract class MonoPoolBase : Injectable
	{
		private GameObjectInjector _injector;
		private GameObjectLifeCycleActionCaller<Initializable> _gameObjectInitializer;
		private GameObjectLifeCycleActionCaller<Cleanable> _gameObjectCleaner;
		private GameObjectContextsReseter _reseter;
		protected MonoPoolCache _cache;
		private ObjectActivator _objectActivator;

		public virtual void Inject(Resolver resolver)
		{
			_injector = resolver.Resolve<GameObjectInjector>();
			_reseter = resolver.Resolve<GameObjectContextsReseter>();
			_cache = resolver.Resolve<MonoPoolCache>();
			_objectActivator = resolver.Resolve<ObjectActivator>();
			_gameObjectInitializer = resolver.Resolve<GameObjectLifeCycleActionCaller<Initializable>>();
			_gameObjectCleaner = resolver.Resolve<GameObjectLifeCycleActionCaller<Cleanable>>();
		}

		protected TItem TakeItem<TItem>(Resolver resolver, int prefabHash, 
			PrefabInstantiationArguments instantiationArguments) where TItem : Component
		{
			TItem item = _cache.Take<TItem>(prefabHash);
			Transform itemTransform = item.transform;
			InitItem(itemTransform, instantiationArguments);
			_injector.InjectIntoContextHierarchy(itemTransform, resolver);
			_gameObjectInitializer.PerformActionOnHierarchy(itemTransform);
			_objectActivator.Activate(item.gameObject);
			return item;
		}

		public void Return<TItem>(TItem item, int prefabHash) where TItem : Component
		{
			GameObject gameObject = item.gameObject;
			_gameObjectCleaner.PerformActionOnHierarchy(item.transform);
			_objectActivator.Disable(gameObject);
			_reseter.Reset(gameObject);
			_cache.Store(prefabHash, item);
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
		
		protected bool HasStoredItem(int prefabHash) => _cache.HasObjects(prefabHash);
	}
    
    public abstract class MonoPoolBase<TItem> : MonoPoolBase where TItem : Component
	{
		private TItem _prefab;
		private int _prefabHash;

		public override void Inject(Resolver resolver)
		{
			base.Inject(resolver);
			_prefab = resolver.Resolve<TItem>();
			_prefabHash = _prefab.GetHashCode();
		}

		protected TItem TakeItem(Resolver resolver, PrefabInstantiationArguments instantiationArguments)
		{
			return TakeItem<TItem>(resolver, _prefabHash, instantiationArguments);
		}

		public void Return(TItem item)
		{
			Return(item, _prefabHash);
		}
		
		protected bool HasStoredItem() => _cache.HasObjects(_prefabHash);
	}
}
