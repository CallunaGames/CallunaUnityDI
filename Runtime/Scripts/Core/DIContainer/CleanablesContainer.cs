using System;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

namespace Calluna.DI
{
    internal class CleanablesContainer
    {
        public IEnumerable<Cleanable> Values => _cleanables;
        private HashSet<Cleanable> _cleanables = new();

        public void Clean()
        {
            foreach (Cleanable cleanable in _cleanables)
            {
                TryClean(cleanable);
            }
        }

        private void TryClean(Cleanable cleanable)
        {
            try
            {
                cleanable.Clean();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void TryAdd(Cleanable cleanable)
        {
            _cleanables.Add(cleanable);
        }

        public void TryAdd(IEnumerable<Cleanable> cleanables)
        {
            foreach (Cleanable cleanable in cleanables)
            {
                TryAdd(cleanable);
            }
        }

        public void Clear()
        {
            _cleanables.Clear();
        }
    }
}