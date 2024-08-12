using UnityEngine;

namespace SBaier.DI.Examples.Pooling
{
    public class Tree : MonoBehaviour, AbstractPoolItem<TreeType>, Injectable
    {
        [field: SerializeField]
        public TreeType ItemId { get; private set; }
        
        public int BranchesAmount { get; private set; }

        public void Inject(Resolver resolver)
        {
            BranchesAmount = resolver.Resolve<Arguments>().BranchesAmount;
        }

        public class Arguments
        {
            public int BranchesAmount;
        }
    }
}