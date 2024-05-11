using UnityEngine;

namespace SBaier.DI
{
	public class MonoPool<TItem> : MonoPoolBase<TItem>, Pool<TItem>, Pool<TItem, PrefabInstantiationArguments> where TItem : Component
    {
        private Factory<TItem, PrefabInstantiationArguments> _factory;
		private Resolver _resolver;

		public override void Inject(Resolver resolver)
		{
			base.Inject(resolver);
			_factory = resolver.Resolve<Factory<TItem, PrefabInstantiationArguments>>();
			_resolver = resolver;
		}

		public TItem Request()
		{
			return Request(default);
		}

		public TItem Request(PrefabInstantiationArguments instantiationArguments)
		{
			return !HasStoredItem ? _factory.Create(instantiationArguments) : TakeItem(_resolver);
		}
    }

	public class MonoPool<TItem, TArg> : MonoPoolBase<TItem>, Pool<TItem, TArg>, Pool<TItem, TArg, PrefabInstantiationArguments> where TItem : Component
	{
		private const int _argumentsCount = 1;
		
		private Factory<TItem, TArg, PrefabInstantiationArguments> _factory;
		private Resolver _baseResolver;

		public override void Inject(Resolver resolver)
		{
			base.Inject(resolver);
			_factory = resolver.Resolve<Factory<TItem, TArg, PrefabInstantiationArguments>>();
			_baseResolver = resolver;
		}

		public TItem Request(TArg arg)
		{
			return !HasStoredItem ? _factory.Create(arg, default) : TakeItem(CreateResolver(arg));
		}

		public TItem Request(TArg arg, PrefabInstantiationArguments instantiationArguments)
		{
			return !HasStoredItem ? _factory.Create(arg, instantiationArguments) : TakeItem(CreateResolver(arg));
		}

		private ArgumentsResolver CreateResolver(TArg arg)
		{
			ArgumentsResolver resolver = new ArgumentsResolver(_baseResolver, _argumentsCount);
			resolver.AddArgument(arg);
			return resolver;
		}
	}
}
