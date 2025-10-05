using System;
using System.Collections.Generic;
using UnityEngine;

namespace Calluna.DI
{
    public class AbstractMonoPoolBase<TItem, TComparable> : MonoPoolBase
        where TItem : Component, AbstractPoolItem<TComparable>
        where TComparable : IComparable
    {
        
        protected PrefabFactory _prefabFactory;
        protected readonly Dictionary<IComparable, TItem> _idToPrefab = new Dictionary<IComparable, TItem>();
        protected readonly Dictionary<IComparable, int> _idToPrefabHash = new Dictionary<IComparable, int>();
        protected Resolver _resolver;

        public override void Inject(Resolver resolver)
        {
            base.Inject(resolver);
            _prefabFactory = resolver.Resolve<PrefabFactory>();
            _resolver = resolver;
        }

        public void AddPrefab(TItem prefab)
        {
            TComparable id = prefab.ItemId;

            if (_idToPrefab.ContainsKey(id))
            {
                throw new ArgumentException($"A prefab with id {prefab.ItemId} has already been added to the pool.");
            }
            
            _idToPrefab.Add(prefab.ItemId, prefab);
            _idToPrefabHash.Add(prefab.ItemId, prefab.GetHashCode());
        }
        
        public void Return(TItem item)
        {
            Return(item, _idToPrefab[item.ItemId].GetHashCode());
        }
    }
}