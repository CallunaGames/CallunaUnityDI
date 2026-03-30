using System;
using UnityEngine;

namespace Calluna.DI
{
    public class CircularDependencyException : Exception
    {
        public CircularDependencyException(BindingKey bindingKey) : base($"Circular dependency detected while resolving contract {bindingKey}") { }
    }
}
