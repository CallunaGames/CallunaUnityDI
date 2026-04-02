using System;
using UnityEngine;

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
