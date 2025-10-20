using UnityEngine;

namespace Calluna.DI.Examples
{
	public class FooInstaller : MonoInstaller
	{
		public override void InstallBindings(Binder binder)
		{
			binder.Bind<Foo>().ToComponent<Foo>().FromNewComponentOnNewGameObject("Foo", transform, false).AsSingle().NonLazy();
		}
	}
}
