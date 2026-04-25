using Calluna;

namespace Calluna.DI.Examples.EventBus
{
    public class WalletSystem : Injectable, Initializable, Cleanable
    {
        private IEventBus _eventBus;
        private int       _gold;

        void Injectable.Inject(Resolver resolver)
        {
            _eventBus = resolver.Resolve<IEventBus>();
        }

        void Initializable.Initialize()
        {
            _eventBus.Subscribe<EnemyDied>(OnEnemyDied);
        }

        void Cleanable.Clean()
        {
            _eventBus.Unsubscribe<EnemyDied>(OnEnemyDied);
        }

        private void OnEnemyDied(EnemyDied evt)
        {
            _gold += evt.GoldReward;
            _eventBus.Publish(new GoldChanged { NewTotal = _gold });
        }
    }
}
