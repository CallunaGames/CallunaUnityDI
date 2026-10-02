using System;
using System.Collections.Generic;
using UnityEngine;

namespace Calluna.DI
{
    internal class QuitHandlersContainer
    {
        public IEnumerable<QuitHandler> Values => _quitHandlers;
        // Creation order, so HandleQuit can run in reverse like Dispose.
        private readonly List<QuitHandler> _quitHandlers = new List<QuitHandler>();
        private readonly HashSet<QuitHandler> _added = new HashSet<QuitHandler>();

        public void Add(QuitHandler quitHandler)
        {
            if (_added.Add(quitHandler))
                _quitHandlers.Add(quitHandler);
        }

        public void Add(IEnumerable<QuitHandler> quitHandlers)
        {
            foreach (QuitHandler quitHandler in quitHandlers)
                Add(quitHandler);
        }

        public void Clear()
        {
            _quitHandlers.Clear();
            _added.Clear();
        }

        public void HandleQuit()
        {
            for (int i = _quitHandlers.Count - 1; i >= 0; i--)
                SafeInvoker.Invoke(_quitHandlers[i], quitHandler => quitHandler.HandleQuit());
        }
    }
}
