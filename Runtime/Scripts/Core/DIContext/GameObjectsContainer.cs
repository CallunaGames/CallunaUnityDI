using System.Collections.Generic;
using UnityEngine;

namespace Calluna.DI
{
	public class GameObjectsContainer
	{
		public IEnumerable<GameObject> Values => _objects;
        private HashSet<GameObject> _objects = new HashSet<GameObject>();
        private GameObjectContextsReseter _reseter;

        public GameObjectsContainer(GameObjectContextsReseter reseter)
		{
            _reseter = reseter;
        }

        public void Add(GameObject gameObject)
        {
            _objects.Add(gameObject);
        }

        public void Add(IEnumerable<GameObject> gameObjects)
        {
	        foreach (GameObject obj in gameObjects)
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
			foreach (GameObject gameObject in _objects)
				Destroy(gameObject);
		}

		private void Destroy(GameObject gameObject)
		{
            _reseter.Reset(gameObject);
            UnityEngine.Object.Destroy(gameObject);
		}
	}
}