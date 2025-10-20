using System;
using UnityEngine;

namespace Calluna.DI.Examples.Pooling
{
	internal class Bar : MonoBehaviour, Injectable, Initializable, Cleanable
	{
		private Foo _foo;
		private int _number;

		public void Inject(Resolver resolver)
		{
			Arguments args = resolver.Resolve<Arguments>();
			_foo = args.Foo;
			_number = args.Num;
		}

		private void OnEnable()
		{
			Debug.Log("Bar OnEnable");
		}

		private void OnDisable()
		{
			Debug.Log("Bar OnDisable");
		}

		public void Initialize()
		{
			Debug.Log("Bar initialized");
		}

		public void Clean()
		{
			Debug.Log("Bar cleaned");
		}
		
		public override string ToString()
		{
			return $"Bar of {_foo.ToString()} with number {_number}.";
		}

		public class Arguments
		{
			public Foo Foo { get; private set; }
			public int Num { get; private set; }

			public Arguments(Foo foo, int num)
			{
				Foo = foo;
				Num = num;
			}
		}
	}
}
