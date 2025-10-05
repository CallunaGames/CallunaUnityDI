using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace Calluna.DI.Examples.Pooling
{
    public class TreesCreator : MonoBehaviour, Injectable, Initializable, Cleanable
    {
        [SerializeField] 
        private int _amount = 10;

        [SerializeField] 
        private Vector2Int _branchesAmountRange = new Vector2Int(10, 20);
        
        private Pool<Tree, TreeType, Tree.Arguments> _pool;
        private TreeType[] _treeTypes;

        private List<Tree> _trees;
        private Random _random;
        
        public void Inject(Resolver resolver)
        {
            _pool = resolver.Resolve<Pool<Tree, TreeType, Tree.Arguments>>();
            _random = new Random();
        }

        public void Initialize()
        {
            _trees = new List<Tree>(_amount);
            _treeTypes = (TreeType[]) Enum.GetValues(typeof(TreeType));
            
            CreateTrees();
        }

        public void Clean()
        {
            foreach (Tree tree in _trees)
            {
                _pool.Return(tree);
            }
        }

        private void CreateTrees()
        {
            for (int i = 0; i < _amount; i++)
            {
                int min = Mathf.Min(_branchesAmountRange.x, _branchesAmountRange.y + 1);
                int max = Mathf.Max(_branchesAmountRange.x, _branchesAmountRange.y + 1);
                int branches = _random.Next(min, max);
                TreeType treeType = _treeTypes[_random.Next(0, _treeTypes.Length)];
                Tree tree = _pool.Request(treeType, new Tree.Arguments() { BranchesAmount = branches });
                Debug.Log($"Created a {tree.ItemId} tree with {tree.BranchesAmount}");
            }
        }
    }
}