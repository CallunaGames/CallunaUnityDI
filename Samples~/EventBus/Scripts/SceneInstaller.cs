namespace Calluna.DI.Examples.EventBus
{
    public class SceneInstaller : MonoInstaller
    {
        public override void InstallBindings(Binder binder)
        {
            binder.BindToNewSelf<EventLogger>().NonLazy();
            binder.BindToNewSelf<WalletSystem>().NonLazy();
            binder.BindToNewSelf<WinConditionChecker>().NonLazy();
            binder.BindToNewSelf<CombatSystem>().NonLazy();
        }
    }
}
