using UnityEngine;

namespace Calluna.DI
{
    internal abstract class GameObjectLifeCycleActionCaller<TActionHolder>
    {
        protected abstract bool Reverse { get; }
        
        public void PerformActionOnHierarchy(Transform transform)
        {
            if(!Reverse)
                PerformAction(transform.GetComponents<TActionHolder>());
            foreach (Transform child in transform)
                PerformActionOnHierarchy(child);
            if(Reverse)
                PerformAction(transform.GetComponents<TActionHolder>());
        }

        private void PerformAction(TActionHolder[] actionHolders)
        {
            foreach (TActionHolder actionHolder in actionHolders)
                CallAction(actionHolder);
        }

        protected abstract void CallAction(TActionHolder actionHolder);
    }
}