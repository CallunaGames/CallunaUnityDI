using UnityEngine;

namespace Calluna.DI.Examples.Pooling
{
	internal class FooInstaller : MonoInstaller
    {
        [SerializeField]
        private Foo _foo;
		[SerializeField]
		private Bar _barPrefab;

		public override void InstallBindings(Binder binder)
		{
			binder.BindInstance(_foo);
			binder.Bind<Factory<Bar, Bar.Arguments>>()
				.And<Factory<Bar, Bar.Arguments, PrefabInstantiationArguments>>()
				.ToNew<PrefabFactory<Bar, Bar.Arguments>>()
				.WithArgument(_barPrefab);
			binder.Bind<Pool<Bar, Bar.Arguments>>()
				.And<Pool<Bar, Bar.Arguments, PrefabInstantiationArguments>>()
				.ToNew<MonoPool<Bar, Bar.Arguments>>()
				.WithArgument(_barPrefab)
				.AsSingle();
		}
	}
}
