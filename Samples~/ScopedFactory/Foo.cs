using System.Collections.Generic;
using UnityEngine;

namespace Calluna.DI.Samples.ScopedFactory
{
    public class Foo : Injectable, Initializable, Cleanable
    {
        private IReadOnlyList<int> _numbers;
        public string Name { get; private set; }
        
        public void Inject(Resolver resolver)
        {
            _numbers = resolver.Resolve<IReadOnlyList<int>>();
            Name = resolver.Resolve<string>();
        }

        public void Initialize()
        {
            Debug.Log($"{Name} initialized. Numbers: {NumbersToString()}");
        }

        public void Clean()
        {
            Debug.Log($"{Name} cleaned.");
        }

        private string NumbersToString()
        {
            string result = "";
            foreach (int number in _numbers)
            {
                result += number + ", ";
            }
            return result;
        }
    }
}
