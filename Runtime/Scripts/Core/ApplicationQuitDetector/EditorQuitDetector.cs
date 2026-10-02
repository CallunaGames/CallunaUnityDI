#if UNITY_EDITOR
using UnityEditor;
#endif

#pragma warning disable CS0618 // QuitDetector is obsolete; kept functional until 2.0.0.

namespace Calluna.DI
{
	internal class EditorQuitDetector : QuitDetector
	{
#if UNITY_EDITOR
		private void Start()
		{
			EditorApplication.playModeStateChanged += OnPlaymodeStateChanged;
		}

		private void OnDestroy()
		{
			EditorApplication.playModeStateChanged -= OnPlaymodeStateChanged;
		}

		private void OnPlaymodeStateChanged(PlayModeStateChange change)
		{

			if (change == PlayModeStateChange.ExitingPlayMode)
				OnApplicationQuitting();
		}
#endif
	}
}
