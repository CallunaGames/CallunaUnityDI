using System;
using System.Collections.Generic;
using UnityEngine;

namespace Calluna.DI
{
    internal class DisposablesContainer
    {
        public IEnumerable<IDisposable> Values => _disposables;
        // Creation order, so Dispose can run in reverse: later instances may depend on earlier ones.
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private readonly HashSet<IDisposable> _added = new HashSet<IDisposable>();

        public void Add(IDisposable disposable)
        {
            if (_added.Add(disposable))
                _disposables.Add(disposable);
        }

        public void Add(IEnumerable<IDisposable> disposables)
        {
            foreach (IDisposable disposable in disposables)
                Add(disposable);
        }

        public void Clear()
        {
            _disposables.Clear();
            _added.Clear();
        }

		internal void Dispose()
		{
            for (int i = _disposables.Count - 1; i >= 0; i--)
                SafeInvoker.Invoke(_disposables[i], disposable => disposable.Dispose());
        }
	}
}
