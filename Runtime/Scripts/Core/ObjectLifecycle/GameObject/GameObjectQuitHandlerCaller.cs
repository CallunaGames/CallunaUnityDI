namespace Calluna.DI
{
    // Same order as GameObjectCleaner: parents before their children.
    internal class GameObjectQuitHandlerCaller : GameObjectLifeCycleActionCaller<QuitHandler>
    {
        protected override bool Reverse => false;

        protected override void CallAction(QuitHandler actionHolder)
        {
            actionHolder.HandleQuit();
        }
    }
}
