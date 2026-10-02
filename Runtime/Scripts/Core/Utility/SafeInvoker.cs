using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Calluna.DI
{
    /// <summary>
    /// Calls an action on many objects in a row - cleaning, disposing, quitting, a hierarchy's lifecycle
    /// calls - where one failing object must not stop the others: its exception is logged instead.
    /// Takes the target as an argument, so a non-capturing lambda or a cached delegate doesn't allocate
    /// (the hierarchy callers run on every pool take and return).
    /// </summary>
    internal static class SafeInvoker
    {
        public static void Invoke<TTarget>(TTarget target, Action<TTarget> action, Object context = null)
        {
            try
            {
                action(target);
            }
            catch (Exception e)
            {
                Debug.LogException(e, context);
            }
        }
    }
}
