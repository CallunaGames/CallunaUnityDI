using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Calluna.DI
{
    /// <summary>
    /// Base of the MonoBehaviour pools. Stored items live in the shared <see cref="MonoPoolCache"/>; a pool
    /// registers there as a user of its prefabs and unregisters when it's disposed - with its context, as
    /// pools are bound as single instances. A scene context's reset then destroys the stored items no
    /// live pool uses anymore.
    /// </summary>
    public abstract class MonoPoolBase : Injectable, IDisposable
	{
		private GameObjectInjector _injector;
		private GameObjectLifeCycleActionCaller<Initializable> _gameObjectInitializer;
		private GameObjectLifeCycleActionCaller<Cleanable> _gameObjectCleaner;
		private GameObjectContextsReseter _reseter;
		protected MonoPoolCache _cache;
		protected ObjectActivator _objectActivator;
		private readonly List<int> _cacheKeys = new List<int>();

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

		/// <summary>Registers the pool as a user of <paramref name="prefabHash"/>'s stored items, once.</summary>
		protected void UseCacheKey(int prefabHash)
		{
			if (_cacheKeys.Contains(prefabHash))
				return;
			_cacheKeys.Add(prefabHash);
			_cache.AddUser(prefabHash);
		}

		/// <summary>
		/// Unregisters the pool from its stored items. Called by its context's reset; the items stay stored
		/// until a scene context destroys the unused ones.
		/// </summary>
		public virtual void Dispose()
		{
			if (_cache != null)
				foreach (int key in _cacheKeys)
					_cache.RemoveUser(key);
			_cacheKeys.Clear();
		}

		// Stored items can be destroyed from outside; call before taking one.
		protected void RemoveDestroyedItems(int prefabHash) => _cache.RemoveDestroyed(prefabHash);
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
			UseCacheKey(_prefabHash);
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

		protected void RemoveDestroyedItems() => _cache.RemoveDestroyed(_prefabHash);

		// Instantiates bare prefab instances with no DI injection or initialisation.
		// Items are stored directly in the cache and will be fully injected on first TakeItem.
		// Avoids paying the DI init cost twice (warm-up + first take) compared to factory-based warm-up.
		protected void WarmUpBare(int count)
		{
			for (int i = 0; i < count; i++)
			{
				TItem instance = Object.Instantiate(_prefab);
				_objectActivator.Disable(instance.gameObject);
				_cache.Store(_prefabHash, instance);
			}
		}
	}
}
