namespace SBaier.DI.Examples.Pooling
{
    public class LifeCycleObjectInstaller : MonoInstaller
    {
        public override void InstallBindings(Binder binder)
        {
            binder.BindToNewSelf<LifeCycleObject>()
                .WithArgument(21)
                .PerRequest();
            binder.BindToNewSelf<LifeCycleObject>("SingleLifeCycleObject")
                .WithArgument(42)
                .AsSingle();
        }
    }
}
