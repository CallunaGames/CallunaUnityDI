using UnityEngine;

#pragma warning disable CS0618 // Tests the obsolete QuitDetector until it's removed in 2.0.0.

namespace Calluna.DI.Tests
{
    public class TestQuitDetector : QuitDetector
    {
        public void SetQuitting()
        {
            OnApplicationQuitting();
        }
    }
}