using System;
using UnityEngine;

namespace Calluna.DI
{
    [Obsolete("Implement QuitHandler instead - it's called while all contexts are still intact. QuitDetector will be removed in 2.0.0.")]
    public abstract class QuitDetector : MonoBehaviour
    {
        public event Action OnQuit;
		public bool IsQuitting { get; private set; } = false;

		protected void OnApplicationQuitting()
		{
			if (IsQuitting)
				return;
			IsQuitting = true;
			OnQuit?.Invoke();
		}

		// Called by the AppContext when it starts quitting, so IsQuitting doesn't depend on whether the
		// detector's own quit event comes earlier or later.
		internal void NotifyQuit() => OnApplicationQuitting();
	}
}
