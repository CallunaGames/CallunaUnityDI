using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SBaier.DI
{
    public abstract class PrefabFactoryBase : Injectable
    {
        private GameObjectInjector _injector;
        private GameObjectInitializer _gameObjectInitializer;

        protected Resolver BaseResolver { get; private set; }

        public virtual void Inject(Resolver resolver)
        {
            _injector = resolver.Resolve<GameObjectInjector>();
            _gameObjectInitializer = resolver.Resolve<GameObjectInitializer>();
            BaseResolver = resolver;
        }

        protected TPrefab CreateInstance<TPrefab>(Resolver resolver, TPrefab prefab,
            PrefabInstantiationArguments args) where TPrefab : Component
        {
            try
            {
                TPrefab result = CreateInstance(args, prefab);
                Transform resultTransform = result.transform;
                _injector.InjectIntoContextHierarchy(resultTransform, resolver);
                _gameObjectInitializer.PerformActionOnHierarchy(resultTransform);
                return result;
            }
            catch (Exception)
            {
                Debug.LogError($"Failed to create an instance of {prefab.name}");
                throw;
            }
        }

        private TPrefab CreateInstance<TPrefab>(PrefabInstantiationArguments args, TPrefab prefab)
            where TPrefab : Component
        {
            TPrefab result;
            if (args.WorldPositionStays.HasValue && args.WorldPositionStays.Value)
            {
                result = Object.Instantiate(prefab, args.Parent, true);
            }
            else
            {
                result = args switch
                {
                    { Position: not null, Rotation: not null } =>
                        Object.Instantiate(prefab, args.Position.Value, args.Rotation.Value, args.Parent),
                    { Position: not null } =>
                        Object.Instantiate(prefab, args.Position.Value, Quaternion.identity, args.Parent),
                    { Rotation: not null } =>
                        Object.Instantiate(prefab, prefab.transform.position, args.Rotation.Value, args.Parent),
                    _ => Object.Instantiate(prefab, args.Parent, false)
                };
            }

            if (args.Scale.HasValue)
            {
                result.transform.localScale = args.Scale.Value;
            }

            if (args.FitRectTransform.HasValue &&
                args.FitRectTransform.Value &&
                result.transform is RectTransform rectTransform)
            {
                rectTransform.sizeDelta = Vector2.one;
                rectTransform.anchoredPosition = Vector2.zero;
            }

            return result;
        }
    }
    
    public abstract class PrefabFactoryBase<TPrefab> : PrefabFactoryBase where TPrefab : Component
    {
        private TPrefab _prefab;

        public override void Inject(Resolver resolver)
        {
            base.Inject(resolver);
            _prefab = resolver.Resolve<TPrefab>();
        }

        protected TPrefab CreateInstance(Resolver resolver,
            PrefabInstantiationArguments args)
        {
            return CreateInstance(resolver, _prefab, args);
        }
    }
}