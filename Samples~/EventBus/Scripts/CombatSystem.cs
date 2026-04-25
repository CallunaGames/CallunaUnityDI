using Calluna;

namespace Calluna.DI.Examples.EventBus
{
    public class CombatSystem : Injectable, Initializable, Cleanable
    {
        private IEventBus _eventBus;

        void Injectable.Inject(Resolver resolver)
        {
            _eventBus = resolver.Resolve<IEventBus>();
        }

        void Initializable.Initialize()
        {
            _eventBus.Subscribe<LevelWon>(OnLevelWon);
            StartWave(1);
        }

        void Cleanable.Clean()
        {
            _eventBus.Unsubscribe<LevelWon>(OnLevelWon);
        }

        private void OnLevelWon(LevelWon evt)
        {
            StartWave(evt.WaveNumber + 1);
        }

        private void StartWave(int wave)
        {
            _eventBus.Publish(new CombatStarted
            {
                WaveNumber = wave,
                EnemyCount = 3 + (wave - 1)
            });
        }
    }
}
