using System;
using System.Collections.Generic;
using UnityEngine;

namespace Calluna.DI
{
    internal abstract class DIContextBase : DIContext, Injectable
    {
        protected DIContainers _containers;
        private DIInstanceFactory _instanceFactory;
        private InstantiationInfoValidator _bindingValidator;
        private GameObjectInjector _gameObjectInjector;
        // The bindings being created, innermost last. Bindings rather than concrete types: two bindings
        // of the same type (e.g. with different IDs) may depend on each other without a cycle.
        private readonly List<Binding> _creationChain = new List<Binding>();
        private readonly GameObjectInitializer _hierarchyInitializer = new GameObjectInitializer();
        private bool _postInit;

        private Resolver _diContainerResolver;
        private Binder _diContainerBinder;

        public Resolver Resolver => _diContainerResolver;
        public Binder Binder => _diContainerBinder;

        private BindingsContainer _bindings => _containers.Bindings;
        private NonLazyContainer _nonLazyBindings => _containers.NonLazyBindings;
        private SingleInstancesContainer _singleInstances => _containers.SingleInstances;
        private DisposablesContainer _disposables => _containers.Disposables;
        private ObjectsContainer _objects => _containers.Objects;
        private GameObjectsContainer _gameObjects => _containers.GameObjectsContainer;
        private CleanablesContainer _cleanables => _containers.CleanablesContainer;
        private QuitHandlersContainer _quitHandlers => _containers.QuitHandlers;

        void Injectable.Inject(Resolver resolver)
        {
            DoInjection(resolver);
            _diContainerResolver = CreateResolver(_bindings);
            _diContainerBinder = new DIContainerBinder(_containers);
            _diContainerBinder.Bind<DIContext>().ToInstance(this);
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

        void DIContext.HandleQuit()
        {
            _quitHandlers.HandleQuit();
        }

        void DIContext.TransferInstancesOf(DIContainers containers)
        {
            _quitHandlers.Add(containers.QuitHandlers.Values);
            _cleanables.TryAdd(containers.CleanablesContainer.Values);
            _disposables.Add(containers.Disposables.Values);
            _objects.Add(containers.Objects.Values);
            _gameObjects.Add(containers.GameObjectsContainer.Values);
        }

        public void PostInit() => _postInit = true;

        protected void RebuildResolver()
        {
            _diContainerResolver = CreateResolver(_bindings);
            _diContainerBinder   = new DIContainerBinder(_containers);
            _diContainerBinder.Bind<DIContext>().ToInstance(this);
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
            return _singleInstances.TryGet<TContract>(binding, out TContract cached)
                ? cached
                : CreateSingleInstance<TContract>(binding);
        }

        private TContract CreateSingleInstance<TContract>(Binding binding)
        {
            TContract instance = CreateInstance<TContract>(binding);
            _singleInstances.Store(binding, instance);
            return instance;
        }

        private TContract CreateInstance<TContract>(Binding binding)
        {
            Type concreteType = binding.ConcreteType;
            if (IsBeingCreated(binding))
                throw new CircularDependencyException(GetCreationChainTypes(concreteType));

            _creationChain.Add(binding);
            try
            {
                TContract instance;
                try
                {
                    instance = _instanceFactory.Create<TContract>(Resolver, binding);
                }
                catch (MissingBindingException e)
                {
                    throw new MissingBindingException(e, concreteType);
                }
                try
                {
                    TryInjection(instance, binding);
                    TryInitialize(instance, binding);
                }
                catch
                {
                    DestroyCreatedObject(instance, binding);
                    throw;
                }
                StoreInstance(instance, binding);
                return instance;
            }
            finally
            {
                _creationChain.RemoveAt(_creationChain.Count - 1);
            }
        }

        // By reference: Binding.Equals compares values, and equal bindings are still different bindings.
        private bool IsBeingCreated(Binding binding)
        {
            foreach (Binding created in _creationChain)
                if (ReferenceEquals(created, binding))
                    return true;
            return false;
        }

        private List<Type> GetCreationChainTypes(Type concreteType)
        {
            List<Type> result = new List<Type>(_creationChain.Count + 1);
            foreach (Binding binding in _creationChain)
                result.Add(binding.ConcreteType);
            result.Add(concreteType);
            return result;
        }

        private void StoreInstance<TContract>(TContract instance, Binding binding)
        {
            InstantiationInfo instantiationInfo = binding;
            if (instantiationInfo.CreationMode == InstanceCreationMode.FromInstance)
                return;
            if (binding.AmountMode == InstanceAmountMode.PerRequest && !binding.TrackInstances)
                return;
            if (instance is IDisposable disposable)
                _disposables.Add(disposable);
            TryAddCleanable(instance);
            // Components are called by the hierarchy traversal of their context.
            if (instance is QuitHandler quitHandler && instance is not Component)
                _quitHandlers.Add(quitHandler);
            if (instance is Component component && !TryAddGameObject(component.gameObject, instantiationInfo))
                _objects.Add(component);
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
        }

        private void TryInjection<TContract>(TContract instance, InstantiationInfo instantiationInfo)
        {
            if (!instantiationInfo.InjectionAllowed)
                return;
            // A component added to an existing GameObject: only it is new - its GameObject's other
            // components and children were injected by their own context already.
            if (instantiationInfo.CreationMode == InstanceCreationMode.FromNewComponentOn)
                InjectIntoInstance(instance, instantiationInfo);
            else if (instance is Component component)
                InjectIntoTransform(component.transform, instantiationInfo);
            else if (instance is GameObject gameObject)
                InjectIntoTransform(gameObject.transform, instantiationInfo);
            else
                InjectIntoInstance(instance, instantiationInfo);
        }

        private void TryInitialize<TContract>(TContract instance, InstantiationInfo instantiationInfo)
        {
            if (instantiationInfo.CreationMode is InstanceCreationMode.FromInstance or InstanceCreationMode.FromFactory)
                return;
            if (instance is Component component)
            {
                // Components resolved before PostInit() is called are initialized by the
                // hierarchy traversers (GameObjectInitializer / SceneObjectsLifeCycleActionCaller).
                // After PostInit(), those traversers have already run, so we initialize directly.
                if (!_postInit)
                    return;
                // A new prefab instance brings its own hierarchy - initialize all of it, like PrefabFactoryBase.
                if (IsPrefabInstance(instantiationInfo.CreationMode))
                {
                    _hierarchyInitializer.PerformActionOnHierarchy(component.transform);
                    return;
                }
            }
            if (instance is Initializable initializable)
                initializable.Initialize();
        }

        private static bool IsPrefabInstance(InstanceCreationMode creationMode) =>
            creationMode is InstanceCreationMode.FromPrefabInstance or InstanceCreationMode.FromResourcePrefabInstance;

        // Injection or initialization failed: the instance is never handed out or stored, so remove the
        // objects created for it instead of leaving them orphaned in the scene.
        private static void DestroyCreatedObject<TContract>(TContract instance, InstantiationInfo instantiationInfo)
        {
            if (instance is not Component component || component == null)
                return;
            switch (instantiationInfo.CreationMode)
            {
                case InstanceCreationMode.FromPrefabInstance:
                case InstanceCreationMode.FromResourcePrefabInstance:
                case InstanceCreationMode.FromNewComponentOnNewGameObject:
                    UnityEngine.Object.Destroy(component.gameObject);
                    break;
                case InstanceCreationMode.FromNewComponentOn:
                    UnityEngine.Object.Destroy(component);
                    break;
            }
        }

        private void TryAddCleanable<TContract>(TContract contract)
        {
            if (contract is not Cleanable cleanable || contract is Component)
                return;
            _cleanables.TryAdd(cleanable);
        }

        private void InjectIntoTransform(Transform transform, InstantiationInfo instantiationInfo)
        {
            _gameObjectInjector.InjectIntoContextHierarchy(transform, GetResolverFor(instantiationInfo));
        }

        private void InjectIntoInstance<TContract>(TContract instance, InstantiationInfo instantiationInfo)
        {
            if (instance is not Injectable injectable)
                return;
            try
            {
                injectable.Inject(GetResolverFor(instantiationInfo));
            }
            catch (MissingBindingException e)
            {
                throw new MissingBindingException(e, instance.GetType());
            }
        }

        private Resolver GetResolverFor(InstantiationInfo instantiationInfo)
        {
            if (instantiationInfo.Arguments.Count == 0)
                return Resolver;
            ArgumentsResolver result = new ArgumentsResolver(Resolver, instantiationInfo.Arguments.Count);
            result.AddArguments(instantiationInfo.Arguments);
            return result;
        }

        protected abstract Resolver CreateResolver(BindingsContainer container);
    }
}