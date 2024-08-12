using UnityEngine;

namespace SBaier.DI
{
    public class PrefabFactory : PrefabFactoryBase
    {
        private const int _argumentsCount = 1;
        
        public TPrefab Create<TPrefab>(TPrefab prefab) where TPrefab : Component
        {
            return CreateInstance(BaseResolver, prefab, default);
        }
        
        public TPrefab Create<TPrefab>(TPrefab prefab, PrefabInstantiationArguments instantiationArgs) where TPrefab : Component
        {
            return CreateInstance(BaseResolver, prefab, instantiationArgs);
        }
        
        public TPrefab Create<TPrefab, TArg>(TPrefab prefab, TArg arg) where TPrefab : Component
        {
            return CreateInstance(CreateResolver(arg), prefab, default);
        }
        
        public TPrefab Create<TPrefab, TArg>(TPrefab prefab, TArg arg, PrefabInstantiationArguments instantiationArgs) where TPrefab : Component
        {
            return CreateInstance(CreateResolver(arg), prefab, instantiationArgs);
        }

        private Resolver CreateResolver<TArg>(TArg arg)
        {
            ArgumentsResolver resolver = new ArgumentsResolver(BaseResolver, _argumentsCount);
            resolver.AddArgument(arg);
            return resolver;
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