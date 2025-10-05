using UnityEngine;

namespace Calluna.DI
{
    internal abstract class GameObjectLifeCycleActionCaller<TActionHolder>
    {
        protected abstract bool Reverse { get; }
        
        public void PerformActionOnHierarchy(Transform transform)
        {
            if(!Reverse)
                Clean(transform.GetComponents<TActionHolder>());
            foreach (Transform child in transform)
                PerformActionOnHierarchy(child);
            if(Reverse)
                Clean(transform.GetComponents<TActionHolder>());
        }

        private void Clean(TActionHolder[] actionHolders)
        {
            foreach (TActionHolder actionHolder in actionHolders)
                CallAction(actionHolder);
        }

        protected abstract void CallAction(TActionHolder actionHolder);
    }
}