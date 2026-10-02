using System;
using System.Collections.Generic;
using UnityEngine;

namespace Calluna.DI
{
	internal class GameObjectsContainer
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
                _objects.Add(obj);
        }

        public void Clear()
        {
            _objects.Clear();
        }

        public void Destroy()
        {
            foreach (GameObject gameObject in _objects)
            {
                // Already destroyed, e.g. together with its scene or parent.
                if (gameObject == null)
                    continue;
                try
                {
                    _reseter.Reset(gameObject);
                }
                catch (Exception e)
                {
                    Debug.LogException(e, gameObject);
                }
                UnityEngine.Object.Destroy(gameObject);
            }
        }
	}
}
