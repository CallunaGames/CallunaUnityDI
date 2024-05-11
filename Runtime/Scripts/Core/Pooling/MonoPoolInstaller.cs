using UnityEngine;

namespace SBaier.DI
{
    public class MonoPoolInstaller<TItem> : MonoInstaller where TItem : Component
    {
        [SerializeField]
        private TItem _prefab;

        public override void InstallBindings(Binder binder)
        {
            binder.Bind<Factory<TItem>>()
                .And<Factory<TItem, PrefabInstantiationArguments>>()
                .ToNew<PrefabFactory<TItem>>()
                .WithArgument(_prefab);
            
            binder.Bind<Pool<TItem>>()
                .And<Pool<TItem, PrefabInstantiationArguments>>()
                .ToNew<MonoPool<TItem>>()
                .WithArgument(_prefab)
                .AsSingle();
        }
    }
    
    public class MonoPoolInstaller<TItem, TArgument> : MonoInstaller where TItem : Component
    {
        [SerializeField]
        private TItem _prefab;

        public override void InstallBindings(Binder binder)
        {
            binder.Bind<Factory<TItem, TArgument>>()
                .And<Factory<TItem, TArgument, PrefabInstantiationArguments>>()
                .ToNew<PrefabFactory<TItem, TArgument>>()
                .WithArgument(_prefab);
            
            binder.Bind<Pool<TItem, TArgument>>()
                .And<Pool<TItem, TArgument, PrefabInstantiationArguments>>()
                .ToNew<MonoPool<TItem, TArgument>>()
                .WithArgument(_prefab)
                .AsSingle();
        }
    }
}
