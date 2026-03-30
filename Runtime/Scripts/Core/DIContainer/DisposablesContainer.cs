using System.Collections.Generic;
using System;

namespace Calluna.DI
{
    public class DisposablesContainer
    {
        public IEnumerable<IDisposable> Values => _disposables;
        private HashSet<IDisposable> _disposables = new HashSet<IDisposable>();

        public void Add(IDisposable disposable)
        {
            _disposables.Add(disposable);
        }

        public void Add(IEnumerable<IDisposable> disposables)
        {
            foreach (IDisposable disposable in disposables)
                _disposables.Add(disposable);
        }

        public void Clear()
        {
            _disposables.Clear();
        }

		internal void Dispose()
		{
            foreach (IDisposable disposable in _disposables)
                disposable.Dispose();
        }
	}
}
