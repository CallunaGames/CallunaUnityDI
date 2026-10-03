using System.Collections.Generic;
using UnityEngine;

namespace Calluna.DI.Tests
{
    // Own file, so Unity finds its script. Keeps its resolver and resolves in Initialize.
    public class ResolvingInitializable : MonoBehaviour, Injectable, Initializable
    {
        private Resolver _resolver;

        public void Inject(Resolver resolver) => _resolver = resolver;

        public void Initialize() => _resolver.Resolve<List<int>>();
    }
}
