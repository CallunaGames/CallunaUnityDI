using System.Collections.Generic;

namespace Calluna.DI
{
    internal class BindingComparer : IEqualityComparer<Binding>
    {
        public bool Equals(Binding x, Binding y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(Binding obj)
        {
            return obj.GetHashCode();
        }
    }
}