using System;
using UnityEngine;

namespace Calluna.DI
{
    public class CircularDependencyException : Exception
    {
        public CircularDependencyException(BindingKey bindingKey) : base($"Cicular dependency detectend while resolving contract {bindingKey}") { }
    }
}
