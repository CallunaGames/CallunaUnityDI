using System;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

namespace Calluna.DI
{
    internal class CleanablesContainer
    {
        public IEnumerable<Cleanable> Values => _cleanables;
        // Creation order. An instance is stored after its injection, so its dependencies come before it -
        // Clean runs in reverse, so every instance is cleaned while its dependencies are still intact
        // (like a GameObject hierarchy, and like Dispose).
        private readonly List<Cleanable> _cleanables = new();
        private readonly HashSet<Cleanable> _added = new();

        public void Clean()
        {
            for (int i = _cleanables.Count - 1; i >= 0; i--)
                TryClean(_cleanables[i]);
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
            if (_added.Add(cleanable))
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
            _added.Clear();
        }
    }
}