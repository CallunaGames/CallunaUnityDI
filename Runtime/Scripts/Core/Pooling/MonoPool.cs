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
			RemoveDestroyedItems();
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
		private Resolver _resolver;

		public override void Inject(Resolver resolver)
		{
			base.Inject(resolver);
			_factory = resolver.Resolve<Factory<TItem, TArg, PrefabInstantiationArguments>>();
			_resolver = resolver;
		}

		public TItem Request(TArg arg)
		{
			return Request(arg, default);
		}

		public TItem Request(TArg arg, PrefabInstantiationArguments instantiationArguments)
		{
			RemoveDestroyedItems();
			return !HasStoredItem() ?
				_factory.Create(arg, instantiationArguments) :
				TakeItem(CreateResolver(arg), instantiationArguments);
		}

		public void WarmUp(int count) => WarmUpBare(count);

		// A new resolver per item: items may keep the resolver and resolve the argument later, so a
		// shared one would hand them the argument of a later request.
		private ArgumentsResolver CreateResolver(TArg arg)
		{
			ArgumentsResolver resolver = new ArgumentsResolver(_resolver, _argumentsCount);
			resolver.AddArgument(arg);
			return resolver;
		}
	}
}
