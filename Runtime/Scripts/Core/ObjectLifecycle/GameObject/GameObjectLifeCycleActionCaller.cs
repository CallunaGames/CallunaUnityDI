using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Calluna.DI
{
    internal abstract class GameObjectLifeCycleActionCaller<TActionHolder>
    {
        // Cached, so calling the action on every component doesn't allocate a delegate.
        private Action<TActionHolder> _callAction;

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
            _callAction ??= CallAction;
            SafeInvoker.Invoke(actionHolder, _callAction, actionHolder as Object);
        }

        protected abstract void CallAction(TActionHolder actionHolder);
    }
}