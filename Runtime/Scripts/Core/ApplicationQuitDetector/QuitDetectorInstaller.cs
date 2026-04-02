using System.Runtime.CompilerServices;
using UnityEngine;

namespace Calluna.DI
{
	internal class QuitDetectorInstaller : Installer
	{
		private readonly GameObject _componentHook;

		public QuitDetectorInstaller(GameObject componentHook)
		{
			_componentHook = componentHook;
		}

		public void InstallBindings(Binder binder)
		{
			if (Application.isEditor)
				binder.Bind<QuitDetector>().ToComponent<EditorQuitDetector>().FromNewComponentOn(_componentHook).AsSingle();
			else
				binder.Bind<QuitDetector>().ToComponent<ApplicationQuitDetector>().FromNewComponentOn(_componentHook).AsSingle();
		}
	}
}
