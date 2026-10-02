using UnityEngine;

namespace Calluna.DI.Tests
{
    // Own file, so Unity finds its script when instantiating a hierarchy that contains it.
    public class InitializableComponent : MonoBehaviour, Initializable
    {
        public int InitializeCount { get; private set; }

        public void Initialize() => InitializeCount++;
    }
}
