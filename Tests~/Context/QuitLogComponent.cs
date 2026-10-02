using System.Collections.Generic;
using UnityEngine;

namespace Calluna.DI.Tests
{
    // Own file, so Unity finds its script.
    public class QuitLogComponent : MonoBehaviour, QuitHandler, Cleanable
    {
        public List<string> Log;

        public void HandleQuit() => Log.Add($"quit {name}");

        public void Clean() => Log.Add($"clean {name}");
    }
}
