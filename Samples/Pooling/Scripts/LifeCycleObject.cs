using UnityEngine;

namespace SBaier.DI.Examples.Pooling
{
    public class LifeCycleObject : Injectable, Initializable, Cleanable
    {
        public int Number { get; private set; }
        
        public void Inject(Resolver resolver)
        {
            Number = resolver.Resolve<int>();
            Debug.Log($"The number {Number} was injected into {nameof(LifeCycleObject)}");
        }

        public void Initialize()
        {
            int former = Number;
            Number *= Number;
            Debug.Log($"The number of {nameof(LifeCycleObject)} changed to {Number} (former: {former}) in initialize");
        }

        public void Clean()
        {
            int former = Number;
            Number = 0;
            Debug.Log($"The number of {nameof(LifeCycleObject)} changed to {Number} (former: {former}) in clean");
        }
    }
}
