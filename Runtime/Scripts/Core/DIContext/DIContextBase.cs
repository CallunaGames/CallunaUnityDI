using System;
using UnityEngine;

namespace Calluna.DI
{
    public abstract class DIContextBase : DIContext, Injectable
    {
        protected DIContainers _containers;
        private DIInstanceFactory _instanceFactory;
        private InstantiationInfoValidator _bindingValidator;
        private GameObjectInjector _gameObjectInjector;

        private Resolver _dIContainerResolver;
        private Binder _dIContainerBinder;

        public Resolver Resolver => _dIContainerResolver;
        public Binder Binder => _dIContainerBinder;

        private BindingsContainer _bindings => _containers.Bindings;
        private NonLazyContainer _nonLazyBindings => _containers.NonLazyInstanceInfos;
        private SingleInstancesContainer _singleInstances => _containers.SingleInstances;
        private DisposablesContainer _disposables => _containers.DisposablesContainer;
        private ObjectsContainer _objects => _containers.ObjectsContainer;
        private GameObjectsContainer _gameObjects => _containers.GameObjectsContainer;
        private CleanablesContainer _cleanables => _containers.CleanablesContainer;

        void Injectable.Inject(Resolver resolver)
        {
            DoInjection(resolver);
            _dIContainerResolver = CreateResolver(_bindings);
            _dIContainerBinder = new DIContainerBinder(_containers);
            _dIContainerBinder.Bind<DIContext>().ToInstance(this);
        }

        protected virtual void DoInjection(Resolver resolver)
        {
            _containers = resolver.Resolve<DIContainers>();
            _instanceFactory = resolver.Resolve<DIInstanceFactory>();
            _bindingValidator = resolver.Resolve<InstantiationInfoValidator>();
            _gameObjectInjector = resolver.Resolve<GameObjectInjector>();
        }

        void DIContext.Clear()
        {
            _containers.Clear();
        }

        void DIContext.Reset()
        {
            _cleanables.Clean();
            _disposables.Dispose();
            _objects.Destroy();
            _gameObjects.Destroy();
            _containers.Clear();
        }

        void DIContext.TransferInstancesOf(DIContainers containers)
        {
            _cleanables.Add(containers.CleanablesContainer.Values);
            _disposables.Add(containers.DisposablesContainer.Values);
            _objects.Add(containers.ObjectsContainer.Values);
            _gameObjects.Add(containers.GameObjectsContainer.Values);
        }

        public void ValidateBindings()
        {
            foreach (InstantiationInfo info in _bindings.GetInstantiationInfos())
                _bindingValidator.Validate(info);
            foreach (Binding binding in _nonLazyBindings.Bindings)
                _bindingValidator.Validate(binding);
        }

        public void CreateNonLazyInstances()
        {
            foreach (Binding binding in _nonLazyBindings.Bindings)
                CreateNonLazyInstance(binding);
        }

        public TContract GetInstance<TContract>(Binding binding)
        {
            _nonLazyBindings.TryAddToCreated(binding);
            return binding.AmountMode switch
            {
                InstanceAmountMode.Single => ResolveSingleInstance<TContract>(binding),
                InstanceAmountMode.PerRequest => CreateInstance<TContract>(binding),
                InstanceAmountMode.Undefined => throw new ArgumentException(),
                _ => throw new NotImplementedException()
            };
        }

        private TContract ResolveSingleInstance<TContract>(Binding binding)
        {
            return _singleInstances.Has(binding)
                ? _singleInstances.Get<TContract>(binding)
                : CreateSingleInstance<TContract>(binding);
        }

        private TContract CreateSingleInstance<TContract>(Binding binding)
        {
            TContract instance = CreateInstance<TContract>(binding);
            _singleInstances.Store(binding, instance);
            return instance;
        }

        private TContract CreateInstance<TContract>(InstantiationInfo instantiationInfo)
        {
            TContract instance = _instanceFactory.Create<TContract>(Resolver, instantiationInfo);
            TryInjection(instance, instantiationInfo);
            TryInitialize(instance, instantiationInfo);
            StoreInstance(instance, instantiationInfo);
            return instance;
        }

        private void StoreInstance<TContract>(TContract instance, InstantiationInfo instantiationInfo)
        {
            if (instantiationInfo.CreationMode == InstanceCreationMode.FromInstance)
                return;
            TryAddDisposable(instance as IDisposable);
            TryAddCleanable(instance);
            if (instance is Component component && !TryAddGameObject(component.gameObject, instantiationInfo))
                _objects.Add(component);
        }

        private void TryAddDisposable(IDisposable disposable)
        {
            if (disposable == null)
                return;
            _disposables.Add(disposable);
        }

        private bool TryAddGameObject(GameObject gameObject, InstantiationInfo instantiationInfo)
        {
            switch (instantiationInfo.CreationMode)
            {
                case InstanceCreationMode.FromPrefabInstance:
                case InstanceCreationMode.FromResourcePrefabInstance:
                case InstanceCreationMode.FromMethod:
                    _gameObjects.Add(gameObject);
                    return true;
            }

            return false;
        }

        private void CreateNonLazyInstance(Binding binding)
        {
            if (!_nonLazyBindings.ShallCreate(binding))
                return;
            if (binding.ConcreteType.IsSubclassOf(typeof(Component)))
                GetInstance<Component>(binding);
            else if (binding.ConcreteType.IsSubclassOf(typeof(UnityEngine.Object)))
                GetInstance<UnityEngine.Object>(binding);
            else
                GetInstance<object>(binding);
            _nonLazyBindings.TryAddToCreated(binding);
        }

        private void TryInjection<TContract>(TContract instance, InstantiationInfo instantiationInfo)
        {
            if (!instantiationInfo.InjectionAllowed)
                return;
            if (instance is Component component)
                InjectIntoComponent(component, instantiationInfo);
            else if (instance is GameObject gameObject)
                InjectIntoGameObject(gameObject, instantiationInfo);
            else
                InjectIntoInstance(instance, instantiationInfo);
        }

        private void TryInitialize<TContract>(TContract instance, InstantiationInfo instantiationInfo)
        {
            if (instantiationInfo.CreationMode == InstanceCreationMode.FromInstance)
                return;
            if (instance is not Initializable initializable || instance is Component)
                return;
            
            try
            {
                initializable.Initialize();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void TryAddCleanable<TContract>(TContract contract)
        {
            if (contract is not Cleanable cleanable || contract is Component)
                return;
            _cleanables.Add(cleanable);
        }

        private void InjectIntoGameObject(GameObject gameObject, InstantiationInfo instantiationInfo)
        {
            InjectIntoTransform(gameObject.transform, instantiationInfo);
        }

        private void InjectIntoComponent(Component component, InstantiationInfo instantiationInfo)
        {
            InjectIntoTransform(component.transform, instantiationInfo);
        }

        private void InjectIntoTransform(Transform transform, InstantiationInfo instantiationInfo)
        {
            _gameObjectInjector.InjectIntoContextHierarchy(transform, GetResolverFor(instantiationInfo));
        }

        private void InjectIntoInstance<TContract>(TContract instance, InstantiationInfo instantiationInfo)
        {
            if (instance is not Injectable injectable)
                return;
            injectable.Inject(GetResolverFor(instantiationInfo));
        }

        private Resolver GetResolverFor(InstantiationInfo instantiationInfo)
        {
            ArgumentsResolver result = new ArgumentsResolver(Resolver);
            result.AddArguments(instantiationInfo.Arguments);
            return result;
        }

        protected abstract Resolver CreateResolver(BindingsContainer container);
    }
}