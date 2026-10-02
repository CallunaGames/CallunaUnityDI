namespace Calluna.DI.Tests
{
    // Own file, so Unity finds its script.
    public class ContextServiceInstaller : MonoInstaller
    {
        public override void InstallBindings(Binder binder)
        {
            binder.BindToNewSelf<ContextService>().AsSingle();
        }
    }

    public class ContextService : Cleanable
    {
        public bool Cleaned { get; private set; }

        public void Clean() => Cleaned = true;
    }
}
