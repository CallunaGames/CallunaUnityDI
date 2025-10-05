using UnityEngine;

namespace Calluna.DI.Examples.CircularDependencyInjection
{
    public class Bar : Injectable
    {
		private Foo _foo;

		public void Inject(Resolver resolver)
		{
			_foo = resolver.Resolve<Foo>();
			Debug.Log(_foo);
		}
	}
}
