using UnityEngine;

namespace Calluna.DI
{
    public abstract class MonoInstaller : MonoBehaviour, Installer
    {
        public abstract void InstallBindings(Binder binder);
    }
}
