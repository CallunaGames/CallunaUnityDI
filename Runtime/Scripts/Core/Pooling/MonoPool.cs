using UnityEngine;

namespace Calluna.DI
{
	public class MonoPool<TItem> : MonoPoolBase<TItem>, Pool<TItem>, Pool<TItem, PrefabInstantiationArguments>,
		WarmablePool<TItem> where TItem : Component
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
			return !HasStoredItem() ?
				_factory.Create(instantiationArguments) :
				TakeItem(_resolver, instantiationArguments);
		}

		public void WarmUp(int count) => WarmUpBare(count);
    }

	public class MonoPool<TItem, TArg> : MonoPoolBase<TItem>, Pool<TItem, TArg>, Pool<TItem, TArg, PrefabInstantiationArguments>,
		WarmablePool<TItem> where TItem : Component
	{
		private const int _argumentsCount = 1;

		private Factory<TItem, TArg, PrefabInstantiationArguments> _factory;
		private ArgumentsResolver _cachedResolver;

		public override void Inject(Resolver resolver)
		{
			base.Inject(resolver);
			_factory = resolver.Resolve<Factory<TItem, TArg, PrefabInstantiationArguments>>();
			_cachedResolver = new ArgumentsResolver(resolver, _argumentsCount);
		}

		public TItem Request(TArg arg)
		{
			return Request(arg, default);
		}

		public TItem Request(TArg arg, PrefabInstantiationArguments instantiationArguments)
		{
			return !HasStoredItem() ?
				_factory.Create(arg, instantiationArguments) :
				TakeItem(UpdateResolver(arg), instantiationArguments);
		}

		public void WarmUp(int count) => WarmUpBare(count);

		private ArgumentsResolver UpdateResolver(TArg arg)
		{
			_cachedResolver.SetArgument(arg);
			return _cachedResolver;
		}
	}
}
