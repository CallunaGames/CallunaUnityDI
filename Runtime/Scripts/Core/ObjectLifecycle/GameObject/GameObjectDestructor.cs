using UnityEngine;

namespace Calluna.DI
{
    public class GameObjectDestructor : Injectable
    {
        private GameObjectLifeCycleActionCaller<Cleanable> _cleanActionCaller;

        public void Inject(Resolver resolver)
        {
            _cleanActionCaller = resolver.Resolve<GameObjectLifeCycleActionCaller<Cleanable>>();
        }

        public void Destruct(GameObject gameObject)
        {
            _cleanActionCaller.PerformActionOnHierarchy(gameObject.transform);
            Object.Destroy(gameObject);
        }
    }
}