using UnityEngine;

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
