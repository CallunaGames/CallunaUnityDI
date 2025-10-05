using UnityEngine;
using UnityEngine.SceneManagement;

namespace Calluna.DI
{
    internal class SceneObjectsLifeCycleActionCaller<TActionHolder> : Injectable
    {
        private GameObjectLifeCycleActionCaller<TActionHolder> _caller;
        
        public void Inject(Resolver resolver)
        {
            _caller = resolver.Resolve<GameObjectLifeCycleActionCaller<TActionHolder>>();
        }
        
        public void PerformActionOnObjectsOf(Scene scene)
        {
            foreach (GameObject sceneObject in scene.GetRootGameObjects())
                _caller.PerformActionOnHierarchy(sceneObject.transform);
        }
    }
}