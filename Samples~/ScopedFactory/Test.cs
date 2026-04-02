using UnityEngine;

namespace Calluna.DI.Samples.ScopedFactory
{
    public class Test : MonoBehaviour, Injectable, Initializable, Cleanable
    {
        private Factory<Bar, Foo> _barFactory;
        private Foo _foo1;
        private Foo _foo2;
        private Bar _bar1;
        private Bar _bar2;
        
        public void Inject(Resolver resolver)
        {
            _barFactory = resolver.Resolve<Factory<Bar, Foo>>();
            _foo1 = resolver.Resolve<Foo>();
            _foo2 = resolver.Resolve<Foo>();
        }

        public void Initialize()
        {
            _bar1 = _barFactory.Create(_foo1);
            _bar2 = _barFactory.Create(_foo2);
        }

        public void Clean()
        {
        }
    }
}