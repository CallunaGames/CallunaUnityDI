using Calluna;
using UnityEngine;
using UnityEngine.UI;

namespace Calluna.DI.Examples.EventBus
{
    [RequireComponent(typeof(Button))]
    public class KillButton : MonoBehaviour, Injectable, Initializable, Cleanable
    {
        [SerializeField] private string _enemyName  = "Goblin";
        [SerializeField] private int    _goldReward = 10;

        private IEventBus _eventBus;
        private Button    _button;

        void Injectable.Inject(Resolver resolver)
        {
            _eventBus = resolver.Resolve<IEventBus>();
        }

        void Initializable.Initialize()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClick);
        }

        void Cleanable.Clean()
        {
            _button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            _eventBus.Publish(new EnemyDied
            {
                EnemyName  = _enemyName,
                GoldReward = _goldReward
            });
        }
    }
}
