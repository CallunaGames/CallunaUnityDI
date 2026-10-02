using UnityEngine;

namespace Calluna.DI.Tests
{
    // Own file, so Unity finds its script when instantiating it.
    public class PoolItem : MonoBehaviour, Injectable, Initializable, Cleanable
    {
        private Resolver _resolver;

        public int InjectCount { get; private set; }
        public int InitializeCount { get; private set; }
        public int CleanCount { get; private set; }
        public PoolItemArgument Argument { get; private set; }

        public void Inject(Resolver resolver)
        {
            InjectCount++;
            _resolver = resolver;
            Argument = resolver.ResolveOptional<PoolItemArgument>();
        }

        public void Initialize() => InitializeCount++;

        public void Clean() => CleanCount++;

        // Resolves the argument again through the resolver kept since injection.
        public PoolItemArgument ResolveArgumentLater() => _resolver.ResolveOptional<PoolItemArgument>();
    }

    public class PoolItemArgument
    {
        public string Name { get; }

        public PoolItemArgument(string name) => Name = name;
    }
}
