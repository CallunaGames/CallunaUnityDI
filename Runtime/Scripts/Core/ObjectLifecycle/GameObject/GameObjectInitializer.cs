namespace Calluna.DI
{
    internal class GameObjectInitializer : GameObjectLifeCycleActionCaller<Initializable>
    {
        protected override bool Reverse => true;

        protected override void CallAction(Initializable actionHolder)
        {
            actionHolder.Initialize();
        }
    }
}