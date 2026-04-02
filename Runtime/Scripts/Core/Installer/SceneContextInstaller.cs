using UnityEngine;

namespace Calluna.DI
{
    internal class SceneContextInstaller : Installer
    {
        private GameObject _contextObject;

        public SceneContextInstaller(GameObject contextObject)
        {
            _contextObject = contextObject;
        }
        
        public void InstallBindings(Binder binder)
        {
            binder.BindInstance(_contextObject.scene);
            binder.Bind<Factory<ChildDIContext, Resolver>>().ToNew<ChildDIContextFactory>();
        }
    }
}

