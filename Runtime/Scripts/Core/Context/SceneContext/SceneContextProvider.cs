using System;
using System.Collections.Generic;
using UnityEngine;

namespace Calluna.DI
{
    internal class SceneContextProvider
    {
        private const string alreadyAddedExceptionMessage = "The scene with iD {0} has already been added to the provider.";
        private const string notAddedExceptionMessage = "The scene with iD {0} has not been added to the provider. Therefore removing it is not possible";
        private const string getExceptionMessage = "The scene with iD {0} has not been added to the provider. Therefore getting it is not possible";
        private const string activeSceneDependenciesExceptionMessage = "The scene with iD {0} is parent to another scene. Unload dependant scenes before removing.";

        private List<SceneContext> _allSceneContexts = new List<SceneContext>();
        private Dictionary<string, SceneContext> _sceneContexts = new Dictionary<string, SceneContext>();
        private Dictionary<string, int> _sceneToDependencyAmount = new Dictionary<string, int>();

        /// <summary>
        /// Set by the AppContext when the application quits: it resets the scene contexts in dependency
        /// order, and a parent may be removed while a child is still registered then.
        /// </summary>
        internal bool IsQuitting { get; set; }

        public void Add(SceneContext context)
        {
            _allSceneContexts.Add(context);
            AddParentDependency(context);
            if (string.IsNullOrEmpty(context.ID))
                return;
            ValidateAdd(context.ID);
            _sceneContexts[context.ID] = context;
        }

		public void Remove(SceneContext context)
        {
            _allSceneContexts.Remove(context);
            RemoveParentDependency(context);
            if (string.IsNullOrEmpty(context.ID))
                return;
            ValidateRemove(context.ID);
            _sceneContexts.Remove(context.ID);
        }

		public SceneContext Get(string iD)
        {
            ValidateGet(iD);
            return _sceneContexts[iD];
        }

        private void AddParentDependency(SceneContext context)
        {
            string parentContextID = context.ParentContextID;
            if (string.IsNullOrEmpty(parentContextID))
                return;
            _sceneToDependencyAmount.TryAdd(parentContextID, 0);
            _sceneToDependencyAmount[parentContextID]++;
        }

        private void RemoveParentDependency(SceneContext context)
        {
            string parentContextID = context.ParentContextID;
            if (string.IsNullOrEmpty(parentContextID))
                return;
            _sceneToDependencyAmount[parentContextID]--;
        }

        private void ValidateAdd(string iD)
        {
            if (_sceneContexts.ContainsKey(iD))
                throw new AlreadyAddedException(iD);
        }

        private void ValidateRemove(string iD)
        {
            if (!_sceneContexts.ContainsKey(iD))
                throw new NotAddedException(iD);
            if (!IsQuitting && IsDependantSceneContext(iD))
                throw new ActiveSceneDependenciesException(iD);
        }

        private bool IsDependantSceneContext(string iD)
		{
            return _sceneToDependencyAmount.ContainsKey(iD) &&
                _sceneToDependencyAmount[iD] > 0;
        }

        internal IReadOnlyList<SceneContext> GetInDependencyOrder()
        {
            // Kahn's algorithm — children (no dependents) before parents.
            // Uses _allSceneContexts so that scenes with empty IDs (leaf children)
            // are included and their dependency on a named parent is respected.
            var counts = new Dictionary<string, int>(_sceneToDependencyAmount);
            var snapshot = new List<SceneContext>(_allSceneContexts);
            var sorted = new List<SceneContext>(snapshot.Count);
            var queue = new Queue<SceneContext>();

            foreach (var ctx in snapshot)
            {
                string id = ctx.ID;
                if (string.IsNullOrEmpty(id) || !counts.TryGetValue(id, out int c) || c == 0)
                    queue.Enqueue(ctx);
            }

            while (queue.Count > 0)
            {
                var ctx = queue.Dequeue();
                sorted.Add(ctx);
                string parentID = ctx.ParentContextID;
                if (!string.IsNullOrEmpty(parentID) && _sceneContexts.TryGetValue(parentID, out var parent))
                {
                    counts.TryGetValue(parentID, out int pc);
                    counts[parentID] = pc - 1;
                    if (counts[parentID] <= 0)
                        queue.Enqueue(parent);
                }
            }

            return sorted;
        }

        private void ValidateGet(string iD)
        {
            if (!_sceneContexts.ContainsKey(iD))
                throw new GetException(iD);
        }

		public class AlreadyAddedException : Exception
        {
            public AlreadyAddedException(string iD) : base(string.Format(alreadyAddedExceptionMessage, iD)) { }
        }

        public class NotAddedException : Exception
        {
            public NotAddedException(string iD) : base(string.Format(notAddedExceptionMessage, iD)) { }
        }

        public class GetException : Exception
        {
            public GetException(string iD) : base(string.Format(getExceptionMessage, iD)) { }
        }

        public class ActiveSceneDependenciesException : Exception
        {
            public ActiveSceneDependenciesException(string iD) : base(string.Format(activeSceneDependenciesExceptionMessage, iD)) { }
        }
    }
}
