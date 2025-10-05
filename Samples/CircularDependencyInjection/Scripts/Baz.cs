using UnityEngine;

namespace Calluna.DI.Examples.CircularDependencyInjection
{
	public class Baz : MonoBehaviour, Injectable
	{
		private Bar _bar;

		public void Inject(Resolver resolver)
		{
			_bar = resolver.Resolve<Bar>();
		}
	}
}
