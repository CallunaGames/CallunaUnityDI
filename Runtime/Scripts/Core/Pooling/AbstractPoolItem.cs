using System;

namespace Calluna.DI
{
    public interface AbstractPoolItem<out TComparable> where TComparable : IComparable
    {
        public TComparable ItemId { get; }
    }
}