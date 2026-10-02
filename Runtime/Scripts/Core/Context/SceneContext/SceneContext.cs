using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Calluna.DI
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-9999)]
    internal class SceneContext : MonoContext
    {
        private ChildDIContext _dIContext;
        protected override DIContext DIContext => _dIContext;

        [SerializeField]
        private string _iD = string.Empty;
        [SerializeField]
        private string _parentContextID = string.Empty;

        private SceneInjector _injector;
        private Scene _scene;
        private SceneContextProvider _sceneContextProvider;
        private SceneObjectsLifeCycleActionCaller<Cleanable> _sceneCleaner;
        private SceneObjectsLifeCycleActionCaller<Initializable> _sceneInitializer;
        private SceneObjectsLifeCycleActionCaller<QuitHandler> _sceneQuitHandler;

        public string ID => _iD;
        public string ParentContextID => _parentContextID;

        private void Awake()
        {
            AppContext appContext = new AppContextProvider().FindOrCreateAppContext();
            Resolver parentResolver = appContext.GetResolverFor(this);
            Init(parentResolver);
        }

        protected override void DoInit(Resolver resolver)
		{
            _dIContext = CreateDIContext(resolver);
            InstallSceneContextBindings();
			ResolveDependencies();
            _sceneContextProvider.Add(this);
        }

        private ChildDIContext CreateDIContext(Resolver resolver)
		{
            Factory<ChildDIContext, Resolver> contextFactory = resolver.Resolve<Factory<ChildDIContext, Resolver>>();
            return contextFactory.Create(resolver);
        }

		private void InstallSceneContextBindings()
        {
            SceneContextInstaller installer = new SceneContextInstaller(gameObject);
            installer.InstallBindings(_binder);
        }

        private void ResolveDependencies()
        {
            _injector = _resolver.Resolve<SceneInjector>();
            _scene = _resolver.Resolve<Scene>();
            _sceneContextProvider = _resolver.Resolve<SceneContextProvider>();
            _sceneCleaner = _resolver.Resolve<SceneObjectsLifeCycleActionCaller<Cleanable>>();
            _sceneInitializer = _resolver.Resolve<SceneObjectsLifeCycleActionCaller<Initializable>>();
            _sceneQuitHandler = _resolver.Resolve<SceneObjectsLifeCycleActionCaller<QuitHandler>>();
        }

        protected override void DoInjection()
        {
            _injector.InjectIntoRootObjectsOf(_scene, _resolver);
        }

        protected override void InitializeObjects()
        {
            _sceneInitializer.PerformActionOnObjectsOf(_scene);
        }

        protected override void CleanObjects()
        {
            _sceneCleaner.PerformActionOnObjectsOf(_scene);
            ResetGameObjectContexts();
        }

        protected override void HandleQuitOfObjects()
        {
            _sceneQuitHandler.PerformActionOnObjectsOf(_scene);
            // The components below a GameObjectContext were handled with the scene above.
            foreach (GameObjectContext context in GetGameObjectContextsChildrenFirst())
                if (IsActive(context))
                    SafeInvoker.Invoke(context, c => c.HandleQuit(), context);
        }

        // The scene's GameObjectContexts are its children: reset them before the scene's own instances,
        // instead of whenever Unity destroys their objects.
        private void ResetGameObjectContexts()
        {
            foreach (GameObjectContext context in GetGameObjectContextsChildrenFirst())
                if (IsActive(context))
                    SafeInvoker.Invoke<Context>(context, c => c.Reset(), context);
        }

        private List<GameObjectContext> GetGameObjectContextsChildrenFirst()
        {
            List<GameObjectContext> result = new List<GameObjectContext>();
            foreach (GameObject rootObject in _scene.GetRootGameObjects())
                result.AddRange(rootObject.GetComponentsInChildren<GameObjectContext>(true));
            // GetComponentsInChildren lists parents before their children.
            result.Reverse();
            return result;
        }

        private static bool IsActive(GameObjectContext context) => context != null && context.IsInitialized;

        protected override ContextAlreadyIsInitializedException CreateContextAlreadyIsInitializedException()
        {
            return new SceneContextAlreadyIsInitializedException(name);
        }

        protected override void DoReset()
        {
            _sceneContextProvider.Remove(this);
            base.DoReset();
        }
    }
}

