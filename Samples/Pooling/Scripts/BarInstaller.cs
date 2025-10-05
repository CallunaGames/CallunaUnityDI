using UnityEngine;

namespace Calluna.DI.Examples.Pooling
{
	internal class BarInstaller : MonoInstaller
	{
		[SerializeField]
		private Bar _bar;

		public override void InstallBindings(Binder binder)
		{
			binder.BindInstance(_bar);
		}
	}
}
