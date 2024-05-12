using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SBaier.DI
{
	public abstract class PrefabFactoryBase<TPrefab> : Injectable where TPrefab : Component
	{
		private GameObjectInjector _injector;
		private GameObjectInitializer _gameObjectInitializer;
		private TPrefab _prefab;

		protected Resolver BaseResolver { get; private set; }

		public virtual void Inject(Resolver resolver)
		{
			_injector = resolver.Resolve<GameObjectInjector>();
			_prefab = resolver.Resolve<TPrefab>();
			_gameObjectInitializer = resolver.Resolve<GameObjectInitializer>();
			BaseResolver = resolver;
		}

		protected TPrefab CreateInstance(Resolver resolver, 
			PrefabInstantiationArguments args)
		{
			try
			{
				TPrefab result = CreateInstance(args);
				Transform resultTransform = result.transform;
				_injector.InjectIntoContextHierarchy(resultTransform, resolver);
				_gameObjectInitializer.PerformActionOnHierarchy(resultTransform);
				return result;
			}
			catch (Exception)
			{
				Debug.LogError($"Failed to create an instance of {_prefab.name}");
				throw;
			}
		}

		private TPrefab CreateInstance(PrefabInstantiationArguments args)
		{
			TPrefab result;
			if (args.WorldPositionStays.HasValue && args.WorldPositionStays.Value)
			{
				result = Object.Instantiate(_prefab, args.Parent, true);
			}
			else
			{
				result = args switch
				{
					{ Position: not null, Rotation: not null } =>
						Object.Instantiate(_prefab, args.Position.Value, args.Rotation.Value, args.Parent),
					{ Position: not null } =>
						Object.Instantiate(_prefab, args.Position.Value, Quaternion.identity, args.Parent),
					{ Rotation: not null } =>
						Object.Instantiate(_prefab, _prefab.transform.position, args.Rotation.Value, args.Parent),
					_ => Object.Instantiate(_prefab, args.Parent, false)
				};
			}

			if (args.Scale.HasValue)
			{
				result.transform.localScale = args.Scale.Value;
			}

			if (args.FitRectTransform.HasValue &&
			    args.FitRectTransform.Value &&
			    result.transform is RectTransform rectTransform)
			{
				rectTransform.sizeDelta = Vector2.one;
				rectTransform.anchoredPosition = Vector2.zero;
			}

			return result;
		}
	}

	public class PrefabFactory<TPrefab> : 
		PrefabFactoryBase<TPrefab>, 
		Factory<TPrefab>, 
		Factory<TPrefab, PrefabInstantiationArguments> where TPrefab : Component
	{
		public TPrefab Create()
		{
			return CreateInstance(BaseResolver, default);
		}

		public TPrefab Create(PrefabInstantiationArguments instantiationArgs)
		{
			return CreateInstance(BaseResolver, instantiationArgs);
		}
	}

	public class PrefabFactory<TPrefab, TArg> : 
		PrefabFactoryBase<TPrefab>, 
		Factory<TPrefab, TArg>,
		Factory<TPrefab, TArg, PrefabInstantiationArguments> where TPrefab : Component
	{
		private const int _argumentsCount = 1;

		public TPrefab Create(TArg arg)
		{
			return CreateInstance(CreateResolver(arg), default);
		}

		public TPrefab Create(TArg arg, PrefabInstantiationArguments instantiationArgs)
		{
			return CreateInstance(CreateResolver(arg), instantiationArgs);
		}

		private Resolver CreateResolver(TArg arg)
		{
			ArgumentsResolver resolver = new ArgumentsResolver(BaseResolver, _argumentsCount);
			resolver.AddArgument(arg);
			return resolver;
		}
	}
}
