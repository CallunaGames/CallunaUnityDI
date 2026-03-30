using System;
using System.Collections.Generic;
using UnityEngine;

namespace Calluna.DI
{
	public class MonoPoolCache : MonoBehaviour
	{
		private Transform _hook;
		private RectTransform _uiHook;
		private Dictionary<int, Stack<GameObject>> _cache = new Dictionary<int, Stack<GameObject>>();

		private void Awake()
		{
			_hook = transform;
			_uiHook = (RectTransform) GetComponentInChildren<Canvas>().transform;
		}

		private void OnDestroy()
		{
			Clear();
		}

		public bool HasObjects(int key)
		{
			return _cache.TryGetValue(key, out Stack<GameObject> stack) && stack.Count > 0;
		}

		public void Store<TComponent>(int key, TComponent component) where TComponent : Component
		{
			Stack<GameObject> objects;
			if (!_cache.TryGetValue(key, out objects))
			{
				objects = new Stack<GameObject>();
				_cache[key] = objects;
			}
			component.transform.SetParent(GetHookFor(component), false);
			objects.Push(component.gameObject);
		}

		private Transform GetHookFor<TComponent>(TComponent component) where TComponent : Component
		{
			return component.transform is RectTransform ? _uiHook : _hook;
		}

		public TComponent Take<TComponent>(int key) where TComponent : Component
		{
			if (!_cache.TryGetValue(key, out Stack<GameObject> stack) || stack.Count == 0)
				throw new EmptyCacheException();
			GameObject obj = stack.Pop();
			TComponent component = obj.GetComponent<TComponent>();
			if (component == null)
				throw new MissingComponentException();
			return component;
		}

		private void Clear()
		{
			foreach (KeyValuePair<int, Stack<GameObject>> entry in _cache)
				foreach (GameObject obj in entry.Value)
					Destroy(obj);
			_cache.Clear();
		}

		private class EmptyCacheException : InvalidOperationException {}

		private class MissingComponentException : InvalidOperationException {}
	}
}
