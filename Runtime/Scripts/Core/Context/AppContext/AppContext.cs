using UnityEngine;

namespace Calluna.DI
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10000)]
    public class AppContext : MonoContext
    {
        private BasicDIContext _dIContext;
        protected override DIContext DIContext => _dIContext;
        private SceneContextProvider _sceneContextProvider;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Init(new Bootstrapper().Resolver);
        }

        protected override void DoInit(Resolver resolver)
        {
            _dIContext = resolver.Resolve<BasicDIContext>();
            InstallAppContextBindings();
            _sceneContextProvider = _resolver.Resolve<SceneContextProvider>();
        }

        private void InstallAppContextBindings()
        {
            AppContextInstaller installer = new AppContextInstaller(gameObject);
            installer.InstallBindings(_binder);
        }

        protected override void DoInjection() { }

        public Resolver GetResolverFor(SceneContext sceneContext)
        {
            string parentID = sceneContext.ParentContextID;
            return string.IsNullOrEmpty(parentID) ? _resolver : _sceneContextProvider.Get(parentID).GetResolver();
        }

		protected override ContextAlreadyIsInitializedException CreateContextAlreadyIsInitializedException()
		{
            return new AppContextAlreadyIsInitializedException(name);
        }
	}
}
