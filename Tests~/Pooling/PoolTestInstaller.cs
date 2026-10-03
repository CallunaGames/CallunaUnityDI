namespace Calluna.DI.Tests
{
    // Own file, so Unity finds its script. Binds a MonoPool<PoolItem> like MonoPoolInstaller does.
    public class PoolTestInstaller : MonoInstaller
    {
        public PoolItem Prefab;

        public override void InstallBindings(Binder binder) => Bind(binder, Prefab);

        public static void Bind(Binder binder, PoolItem prefab)
        {
            binder.Bind<Factory<PoolItem>>()
                .And<Factory<PoolItem, PrefabInstantiationArguments>>()
                .ToNew<PrefabFactory<PoolItem>>()
                .WithArgument(prefab);
            binder.Bind<Pool<PoolItem>>()
                .And<Pool<PoolItem, PrefabInstantiationArguments>>()
                .And<WarmablePool<PoolItem>>()
                .ToNew<MonoPool<PoolItem>>()
                .WithArgument(prefab)
                .AsSingle();
        }
    }
}
