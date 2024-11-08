using System.Collections.Generic;

namespace SBaier.DI
{
    public class CleanablesContainer
    {
        private List<Cleanable> _cleanables = new ();

        public void Clean()
        {
            foreach (Cleanable cleanable in _cleanables)
            {
                cleanable.Clean();
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