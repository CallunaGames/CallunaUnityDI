using System.Collections.Generic;

namespace Calluna.DI
{
    internal class BindingKeyComparer : IEqualityComparer<BindingKey>
    {
        public bool Equals(BindingKey x, BindingKey y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(BindingKey obj)
        {
            return obj.GetHashCode();
        }
    }
}
