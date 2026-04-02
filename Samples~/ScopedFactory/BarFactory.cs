using UnityEngine;

namespace Calluna.DI.Samples.ScopedFactory
{
    public class BarFactory : ScopedFactory<Bar, Foo>
    {
        private static int _number = 0;
        
        protected override void InitScope(Resolver resolver, Binder binder)
        {
            binder.BindInstance("Bar" + _number);
            binder.BindToNewSelf<Bar>();
            _number++;
        }
    }
}