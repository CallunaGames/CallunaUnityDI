using UnityEngine;

namespace Calluna.DI
{
    public class MonoPoolInstaller<TItem> : MonoInstaller, Injectable, Initializable where TItem : Component
    {
        [SerializeField] private TItem _prefab;
        [SerializeField] private int _preWarmCount = 0;

        private Resolver _resolver;

        void Injectable.Inject(Resolver resolver) => _resolver = resolver;

        void Initializable.Initialize()
        {
            if (_preWarmCount > 0)
                _resolver.Resolve<WarmablePool<TItem>>().WarmUp(_preWarmCount);
        }

        public override void InstallBindings(Binder binder)
        {
            binder.Bind<Factory<TItem>>()
                .And<Factory<TItem, PrefabInstantiationArguments>>()
                .ToNew<PrefabFactory<TItem>>()
                .WithArgument(_prefab);

            binder.Bind<Pool<TItem>>()
                .And<Pool<TItem, PrefabInstantiationArguments>>()
                .And<WarmablePool<TItem>>()
                .ToNew<MonoPool<TItem>>()
                .WithArgument(_prefab)
                .AsSingle();
        }
    }

    public class MonoPoolInstaller<TItem, TArgument> : MonoInstaller, Injectable, Initializable where TItem : Component
    {
        [SerializeField] private TItem _prefab;
        [SerializeField] private int _preWarmCount = 0;

        private Resolver _resolver;

        void Injectable.Inject(Resolver resolver) => _resolver = resolver;

        void Initializable.Initialize()
        {
            if (_preWarmCount > 0)
                _resolver.Resolve<WarmablePool<TItem>>().WarmUp(_preWarmCount);
        }

        public override void InstallBindings(Binder binder)
        {
            binder.Bind<Factory<TItem, TArgument>>()
                .And<Factory<TItem, TArgument, PrefabInstantiationArguments>>()
                .ToNew<PrefabFactory<TItem, TArgument>>()
                .WithArgument(_prefab);

            binder.Bind<Pool<TItem, TArgument>>()
                .And<Pool<TItem, TArgument, PrefabInstantiationArguments>>()
                .And<WarmablePool<TItem>>()
                .ToNew<MonoPool<TItem, TArgument>>()
                .WithArgument(_prefab)
                .AsSingle();
        }
    }
}
