using System;
using UnityEngine;

namespace SBaier.DI
{
    public class AbstractMonoPoolInstaller<TItem, TComparable> : MonoInstaller 
        where TItem : Component, AbstractPoolItem<TComparable>
        where TComparable : IComparable
    {
        [SerializeField]
        private TItem[] _prefabs;

        public override void InstallBindings(Binder binder)
        {
            AbstractMonoPool<TItem, TComparable> pool = new AbstractMonoPool<TItem, TComparable>();

            foreach (TItem prefab in _prefabs)
            {
                pool.AddPrefab(prefab);
            }

            binder.Bind<Pool<TItem, TComparable>>()
                .And<Pool<TItem, TComparable, PrefabInstantiationArguments>>()
                .ToInstance(pool);
        }
    }
    
    public class AbstractMonoPoolInstaller<TItem, TComparable, TArg> : MonoInstaller 
        where TItem : Component, AbstractPoolItem<TComparable>
        where TComparable : IComparable
    {
        [SerializeField]
        private TItem[] _prefabs;

        public override void InstallBindings(Binder binder)
        {
            AbstractMonoPool<TItem, TComparable, TArg> pool = new AbstractMonoPool<TItem, TComparable, TArg>();

            foreach (TItem prefab in _prefabs)
            {
                pool.AddPrefab(prefab);
            }

            binder.Bind<Pool<TItem, TComparable, TArg>>()
                .And<Pool<TItem, TComparable, TArg, PrefabInstantiationArguments>>()
                .ToInstance(pool)
                .WithInjection();
        }
    }
}