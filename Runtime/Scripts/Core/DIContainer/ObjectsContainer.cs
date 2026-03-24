using System.Collections.Generic;

namespace Calluna.DI
{
    public class ObjectsContainer
    {
        public IEnumerable<UnityEngine.Object> Values => _objects;
        private HashSet<UnityEngine.Object> _objects = new HashSet<UnityEngine.Object>();

        public void Add(UnityEngine.Object obj)
        {
            _objects.Add(obj);
        }

        public void Add(IEnumerable<UnityEngine.Object> objects)
        {
            foreach (UnityEngine.Object obj in objects)
            {
                _objects.Add(obj);
            }
        }

        public void Clear()
        {
            _objects.Clear();
        }

		public void Destroy()
        {
            foreach (UnityEngine.Object obj in _objects)
                UnityEngine.Object.Destroy(obj);
        }
	}
}
