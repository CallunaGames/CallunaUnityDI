using System.Collections.Generic;
using System;

namespace Calluna.DI
{
    public class DisposablesContainer
    {
        public IEnumerable<IDisposable> Values => _disposibles;
        private HashSet<IDisposable> _disposibles = new HashSet<IDisposable>();

        public void Add(IDisposable disposable)
        {
            _disposibles.Add(disposable);
        }

        public void Add(IEnumerable<IDisposable> disposables)
        {
            foreach (IDisposable disposable in disposables)
                _disposibles.Add(disposable);
        }

        public void Clear()
        {
            _disposibles.Clear();
        }

		internal void Dispose()
		{
            foreach (IDisposable disposable in _disposibles)
                disposable.Dispose();
        }
	}
}
