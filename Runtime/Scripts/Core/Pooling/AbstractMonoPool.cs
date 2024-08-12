using System;
using UnityEngine;

namespace SBaier.DI
{
    public class AbstractMonoPool<TItem, TComparable> : 
        AbstractMonoPoolBase<TItem, TComparable>, 
        Pool<TItem, TComparable>, 
        Pool<TItem, TComparable, PrefabInstantiationArguments>
        where TItem : Component, AbstractPoolItem<TComparable>
        where TComparable : IComparable
    {
        public TItem Request(TComparable id)
        {
            return Request(id, default);
        }

        public TItem Request(TComparable id, PrefabInstantiationArguments instantiationArguments)
        {
            if (!_idToPrefab.TryGetValue(id, out TItem prefab))
            {
                throw new ArgumentException($"There is no prefab with id {id}. " +
                                            $"Please make sure to add it to the factory before calling {nameof(Request)}");
            }
            
            return !HasStoredItem(prefab.GetHashCode())
                ? _prefabFactory.Create(prefab, instantiationArguments)
                : TakeItem<TItem>(_resolver, _idToPrefabHash[id], instantiationArguments);
        }
    }
    
    public class AbstractMonoPool<TItem, TComparable, TArg> : 
        AbstractMonoPoolBase<TItem, TComparable>, 
        Pool<TItem, TComparable, TArg>, 
        Pool<TItem, TComparable, TArg, PrefabInstantiationArguments>
        where TItem : Component, AbstractPoolItem<TComparable>
        where TComparable : IComparable
    {
        private const int _argumentsCount = 1;

        public TItem Request(TComparable id, TArg argument)
        {
            return Request(id, argument, default);
        }

        public TItem Request(TComparable id, TArg argument, PrefabInstantiationArguments instantiationArguments)
        {
            if (!_idToPrefab.TryGetValue(id, out TItem prefab))
            {
                throw new ArgumentException($"There is no prefab with id {id}. " +
                                            $"Please make sure to add it to the factory before calling {nameof(Request)}");
            }
            
            return !HasStoredItem(prefab.GetHashCode())
                ? _prefabFactory.Create(prefab, argument, instantiationArguments)
                : TakeItem<TItem>(CreateResolver(argument), _idToPrefabHash[id], instantiationArguments);
        }

        private ArgumentsResolver CreateResolver(TArg arg)
        {
            ArgumentsResolver resolver = new ArgumentsResolver(_resolver, _argumentsCount);
            resolver.AddArgument(arg);
            return resolver;
        }
    }
}