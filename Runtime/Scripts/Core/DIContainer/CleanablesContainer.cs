using System;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

namespace Calluna.DI
{
    public class CleanablesContainer
    {
        private List<Cleanable> _cleanables = new ();

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

        public void Add(Cleanable cleanable)
        {
            _cleanables.Add(cleanable);
        }
        
        public void Clear()
        {
            _cleanables.Clear();
        }
    }
}