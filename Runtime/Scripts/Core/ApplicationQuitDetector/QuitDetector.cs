using System;
using UnityEngine;

namespace Calluna.DI
{
    public abstract class QuitDetector : MonoBehaviour
    {
        public event Action OnQuit;
		public bool IsQuitting { get; private set; } = false;

		protected void OnApplicationQuitting()
		{
			IsQuitting = true;
			OnQuit?.Invoke();
		}
	}
}
