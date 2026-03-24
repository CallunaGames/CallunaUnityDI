using UnityEngine;

namespace Calluna.DI.Samples.ScopedFactory
{
    public class Bar : Injectable, Initializable, Cleanable
    {
        private Foo _foo;
        private string _name;
        
        public void Inject(Resolver resolver)
        {
            _foo = resolver.Resolve<Foo>();
            _name = resolver.Resolve<string>();
        }

        public void Initialize()
        {
            Debug.Log($"{_name} initialized with Foo {_foo.Name}");
        }

        public void Clean()
        {
            Debug.Log($"{_name} cleaned.");
        }
    }
}