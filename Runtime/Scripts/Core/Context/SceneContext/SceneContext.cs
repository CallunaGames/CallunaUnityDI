using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Calluna.DI
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-9999)]
    public class SceneContext : MonoContext
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
        }

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

