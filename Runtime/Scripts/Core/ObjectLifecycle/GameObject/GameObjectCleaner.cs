namespace SBaier.DI
{
    internal class GameObjectCleaner : GameObjectLifeCycleActionCaller<Cleanable>
    {
        protected override bool Reverse => false;

        protected override void CallAction(Cleanable actionHolder)
        {
            actionHolder.Clean();
        }
    }
}