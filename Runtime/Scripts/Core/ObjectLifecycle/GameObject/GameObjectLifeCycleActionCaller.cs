using System;
using UnityEngine;
using Object = UnityEngine.Object;

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
            {
                PerformAction(actionHolder);
            }
        }

        private void PerformAction(TActionHolder actionHolder)
        {
            try
            {
                CallAction(actionHolder);
            }
            catch (Exception e)
            {
                Debug.LogException(e, actionHolder as Object);
            }
        }

        protected abstract void CallAction(TActionHolder actionHolder);
    }
}