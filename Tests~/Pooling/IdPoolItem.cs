using UnityEngine;

namespace Calluna.DI.Tests
{
    // Own file, so Unity finds its script when instantiating it.
    public class IdPoolItem : MonoBehaviour, AbstractPoolItem<int>, Injectable
    {
        public int Id;

        public int ItemId => Id;
        public int InjectCount { get; private set; }

        public void Inject(Resolver resolver) => InjectCount++;
    }
}
