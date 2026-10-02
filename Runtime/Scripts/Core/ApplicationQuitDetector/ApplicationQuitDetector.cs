using System;
using UnityEngine;

#pragma warning disable CS0618 // QuitDetector is obsolete; kept functional until 2.0.0.

namespace Calluna.DI
{
	internal class ApplicationQuitDetector : QuitDetector
	{
		private void Start()
		{
			Application.quitting += OnApplicationQuitting;
		}

		private void OnDestroy()
		{
			Application.quitting -= OnApplicationQuitting;
		}
	}
}
