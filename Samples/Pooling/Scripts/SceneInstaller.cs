namespace SBaier.DI.Examples.Pooling
{
    public class SceneInstaller : MonoInstaller
    {
        public override void InstallBindings(Binder binder)
        {
            binder.BindToNewSelf<PrefabFactory>();
        }
    }
}