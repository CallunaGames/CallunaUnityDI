using System;

namespace SBaier.DI
{
    public interface AbstractPoolItem<out TComparable> where TComparable : IComparable
    {
        public TComparable ItemId { get; }
    }
}