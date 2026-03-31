using System;
using System.Collections.Generic;
using System.Linq;

namespace Calluna.DI
{
    public class CircularDependencyException : Exception
    {
        public CircularDependencyException(IReadOnlyList<Type> chain)
            : base("Circular dependency detected: " + string.Join(" → ", chain.Select(t => t.Name))) { }
    }
}
