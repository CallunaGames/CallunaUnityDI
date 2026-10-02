namespace Calluna.DI.Tests
{
    // Own file, so Unity finds its script. Installers are injected by their context, not by the
    // hierarchy injection.
    public class SpyMonoInstaller : MonoInstaller, Injectable
    {
        public bool Injected { get; private set; }

        public void Inject(Resolver resolver) => Injected = true;

        public override void InstallBindings(Binder binder) { }
    }
}
