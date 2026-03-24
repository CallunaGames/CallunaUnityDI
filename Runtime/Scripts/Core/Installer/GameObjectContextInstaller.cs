using UnityEngine;

namespace Calluna.DI
{
    public class GameObjectContextInstaller : Installer
    {
        private DIContext _diContext;

        public GameObjectContextInstaller(DIContext dIContext)
        {
            _diContext = dIContext;
        }

        public void InstallBindings(Binder binder)
        {
            
        }
    }
}
