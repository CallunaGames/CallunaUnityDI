using System;
using System.Collections.Generic;
using UnityEngine;

namespace Calluna.DI.Tests
{
    // Own file, so Unity finds its script.
    public class LifecycleLogComponent : MonoBehaviour, Injectable, Initializable, Cleanable
    {
        public List<string> Log;
        public bool Throw;

        public void Inject(Resolver resolver) => Add("inject");

        public void Initialize() => Add("initialize");

        public void Clean() => Add("clean");

        private void Add(string action)
        {
            if (Throw)
                throw new InvalidOperationException($"{name} {action} failed");
            Log.Add($"{action} {name}");
        }
    }
}
