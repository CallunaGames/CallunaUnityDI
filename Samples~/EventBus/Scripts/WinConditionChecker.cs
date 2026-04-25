using Calluna;

namespace Calluna.DI.Examples.EventBus
{
    public class WinConditionChecker : Injectable, Initializable, Cleanable
    {
        private IEventBus _eventBus;
        private int       _remainingEnemies;
        private int       _currentWave;

        void Injectable.Inject(Resolver resolver)
        {
            _eventBus = resolver.Resolve<IEventBus>();
        }

        void Initializable.Initialize()
        {
            _eventBus.Subscribe<CombatStarted>(OnCombatStarted);
            _eventBus.Subscribe<EnemyDied>(OnEnemyDied);
        }

        void Cleanable.Clean()
        {
            _eventBus.Unsubscribe<CombatStarted>(OnCombatStarted);
            _eventBus.Unsubscribe<EnemyDied>(OnEnemyDied);
        }

        private void OnCombatStarted(CombatStarted evt)
        {
            _currentWave      = evt.WaveNumber;
            _remainingEnemies = evt.EnemyCount;
        }

        private void OnEnemyDied(EnemyDied evt)
        {
            _remainingEnemies--;
            if (_remainingEnemies <= 0)
                _eventBus.Publish(new LevelWon { WaveNumber = _currentWave });
        }
    }
}
