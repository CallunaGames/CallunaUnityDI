namespace Calluna.DI.Tests
{
    /// <summary>
    /// A MonoInstaller that also implements Injectable and records what was called and in what order.
    /// </summary>
    internal class SpyInstaller : MonoInstaller, Injectable
    {
        public bool WasInjected { get; private set; }
        public bool WasInstallBindingsCalled { get; private set; }
        public bool WasInjectedBeforeInstallBindings { get; private set; }
        public Resolver InjectedResolver { get; private set; }

        void Injectable.Inject(Resolver resolver)
        {
            WasInjected = true;
            InjectedResolver = resolver;
        }

        public override void InstallBindings(Binder binder)
        {
            WasInstallBindingsCalled = true;
            WasInjectedBeforeInstallBindings = WasInjected;
        }
    }
}
