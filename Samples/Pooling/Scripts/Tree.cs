using UnityEngine;

namespace SBaier.DI.Examples.Pooling
{
    public class Tree : MonoBehaviour, AbstractPoolItem<TreeType>, Injectable
    {
        [field: SerializeField]
        public TreeType ItemId { get; private set; }
        
        public int BranchesAmount { get; private set; }

        private LifeCycleObject _lifeCycleObject;
        private LifeCycleObject _singleLifeCycleObject;

        public void Inject(Resolver resolver)
        {
            BranchesAmount = resolver.Resolve<Arguments>().BranchesAmount;
            _lifeCycleObject = resolver.Resolve<LifeCycleObject>();
            _singleLifeCycleObject = resolver.Resolve<LifeCycleObject>("SingleLifeCycleObject");
            Debug.Log($"Injected {nameof(LifeCycleObject)} with number {_lifeCycleObject.Number} into tree");
            Debug.Log($"Injected single {nameof(LifeCycleObject)} with number {_singleLifeCycleObject.Number} into tree");
        }

        public class Arguments
        {
            public int BranchesAmount;
        }
    }
}