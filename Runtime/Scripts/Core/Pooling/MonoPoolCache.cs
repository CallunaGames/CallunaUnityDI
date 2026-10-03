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
		// Live pools per key. The stash is shared by prefab, so only the cache knows whether any pool
		// may still take an object of a key.
		private readonly Dictionary<int, int> _users = new Dictionary<int, int>();

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

		/// <summary>
		/// Removes the objects stored under <paramref name="key"/> that were destroyed meanwhile and
		/// would be taken next, so <see cref="HasObjects"/> and <see cref="Take{TComponent}"/> only see a
		/// usable object. Only checks the top of the stash - deeper destroyed objects are removed once they
		/// get there - so it stays cheap enough to call before every take.
		/// </summary>
		public void RemoveDestroyed(int key)
		{
			if (!_cache.TryGetValue(key, out Stack<GameObject> stack))
				return;
			while (stack.Count > 0 && stack.Peek() == null)
				stack.Pop();
		}

		/// <summary>A pool that takes and stores objects under <paramref name="key"/> came alive.</summary>
		internal void AddUser(int key)
		{
			_users.TryGetValue(key, out int count);
			_users[key] = count + 1;
		}

		/// <summary>A pool of <paramref name="key"/> was disposed (its context was reset).</summary>
		internal void RemoveUser(int key)
		{
			if (!_users.TryGetValue(key, out int count))
				return;
			if (count > 1)
				_users[key] = count - 1;
			else
				_users.Remove(key);
		}

		/// <summary>
		/// Destroys the stored objects no live pool uses anymore - e.g. those of a scene that was unloaded.
		/// Called by a scene context after its reset; not by GameObjectContexts, whose pools are disposed
		/// on every return of the pooled object and recreated on its next take, reusing the stored objects.
		/// </summary>
		internal void DestroyUnused()
		{
			List<int> unused = null;
			foreach (int key in _cache.Keys)
				if (!_users.ContainsKey(key))
					(unused ??= new List<int>()).Add(key);
			if (unused == null)
				return;
			foreach (int key in unused)
			{
				foreach (GameObject obj in _cache[key])
					if (obj != null)
						Destroy(obj);
				_cache.Remove(key);
			}
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
					if (obj != null)
						Destroy(obj);
			_cache.Clear();
		}

		private class EmptyCacheException : InvalidOperationException {}

		private class MissingComponentException : InvalidOperationException {}
	}
}
