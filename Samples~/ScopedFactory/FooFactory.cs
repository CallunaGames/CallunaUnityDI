using System.Collections.Generic;
using UnityEngine;

namespace Calluna.DI.Samples.ScopedFactory
{
    public class FooFactory : ScopedFactory<Foo>
    {
        private static int _number = 0;

        protected override void InitScope(Resolver resolver, Binder binder)
        {
            int numbersAmount = resolver.Resolve<int>();
            Vector2Int numbersRange = resolver.Resolve<Vector2Int>();
            binder.BindInstance("Foo" + _number);
            binder.Bind<IReadOnlyList<int>>().ToInstance(CreateNumbers(numbersAmount, numbersRange));
            binder.BindToNewSelf<Foo>();
            _number++;
        }

        private IReadOnlyList<int> CreateNumbers(int numbersAmount, Vector2Int numbersRange)
        {
            List<int> result = new List<int>();
            for (int i = 0; i < numbersAmount; i++)
            {
                result.Add(UnityEngine.Random.Range(numbersRange.x, numbersRange.y));   
            }
            return result;
        }
    }
}
