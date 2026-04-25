using Calluna;
using UnityEngine;

namespace Calluna.DI.Examples.EventBus
{
    public class EventLogger : Injectable, Initializable, Cleanable
    {
        private IEventBus _eventBus;

        void Injectable.Inject(Resolver resolver)
        {
            _eventBus = resolver.Resolve<IEventBus>();
        }

        void Initializable.Initialize()
        {
            _eventBus.Subscribe<CombatStarted>(OnCombatStarted);
            _eventBus.Subscribe<EnemyDied>(OnEnemyDied);
            _eventBus.Subscribe<GoldChanged>(OnGoldChanged);
            _eventBus.Subscribe<LevelWon>(OnLevelWon);
        }

        void Cleanable.Clean()
        {
            _eventBus.Unsubscribe<CombatStarted>(OnCombatStarted);
            _eventBus.Unsubscribe<EnemyDied>(OnEnemyDied);
            _eventBus.Unsubscribe<GoldChanged>(OnGoldChanged);
            _eventBus.Unsubscribe<LevelWon>(OnLevelWon);
        }

        private void OnCombatStarted(CombatStarted evt) =>
            Debug.Log($"[EventBus] Wave {evt.WaveNumber} started — {evt.EnemyCount} enemies incoming.");

        private void OnEnemyDied(EnemyDied evt) =>
            Debug.Log($"[EventBus] {evt.EnemyName} died — +{evt.GoldReward} gold.");

        private void OnGoldChanged(GoldChanged evt) =>
            Debug.Log($"[EventBus] Gold: {evt.NewTotal}.");

        private void OnLevelWon(LevelWon evt) =>
            Debug.Log($"[EventBus] Wave {evt.WaveNumber} cleared!");
    }
}
